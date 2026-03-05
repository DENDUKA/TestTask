using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using task.Data;
using task.Entities;
using task.Entities.Dto;

namespace task.Services;

public class ImportService(ILogger<ImportService> logger, DellinDictionaryDbContext context)
{
    private const string FilesDirectory = "files";
    private const string TerminalsFile = "terminals.json";
    private const string DefaultCountryCode = "RU";

    private readonly ILogger<ImportService> _logger = logger;
    private readonly DellinDictionaryDbContext _context = context;

    public async Task ImportAsync(CancellationToken ct = default)
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FilesDirectory, TerminalsFile);
            if (!File.Exists(filePath))
            {
                _logger.LogError("Файл не найден: {FilePath}", filePath);
                return;
            }

            _logger.LogInformation("Чтение файла {FilePath}...", filePath);

            RootDto? root;
            await using (var fileStream = File.OpenRead(filePath))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                root = await JsonSerializer.DeserializeAsync<RootDto>(fileStream, options, ct);
            }

            if (root?.City == null)
            {
                _logger.LogWarning("JSON пуст или имеет неверную структуру.");
                return;
            }

            var offices = new List<Office>();

            foreach (var city in root.City)
            {
                if (city.Terminals?.Terminal == null) continue;

                foreach (var t in city.Terminals.Terminal)
                {
                    var office = MapToOffice(t, city);
                    offices.Add(office);
                }
            }

            _logger.LogInformation("Загружено {Count} терминалов из JSON", offices.Count);

            await UpdateDatabaseAsync(offices, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта: {Message}", ex.Message);
        }
    }

    private Office MapToOffice(TerminalDto t, CityDto city)
    {
        var office = new Office
        {
            Code = t.Id,
            CityCode = city.Id ?? 0,
            Uuid = Guid.NewGuid().ToString(),
            Type = t.IsPVZ ? OfficeType.PVZ : OfficeType.WAREHOUSE,
            CountryCode = DefaultCountryCode,
            Coordinates = new Coordinates
            {
                Latitude = double.TryParse(t.Latitude, out var lat) ? lat : 0,
                Longitude = double.TryParse(t.Longitude, out var lon) ? lon : 0
            },
            AddressCity = city.Name,
            AddressStreet = t.Address,
            WorkTime = t.WorkTime
        };

        if (t.Phones != null)
        {
            foreach (var pDto in t.Phones)
            {
                office.Phones.Add(new Phone
                {
                    PhoneNumber = pDto.Number,
                    Additional = pDto.Comment,
                    Office = office
                });
            }
        }

        return office;
    }

    private async Task UpdateDatabaseAsync(List<Office> offices, CancellationToken ct)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            _logger.LogInformation("Очистка старых записей...");
            
            // Удаляем данные. Каскадное удаление настроено в DbContext, но ExecuteDelete не вызывает его автоматически.
            // Поэтому удаляем телефоны явно, чтобы избежать ошибок внешнего ключа, если каскад не сработает на уровне БД.
            await _context.Phones.ExecuteDeleteAsync(ct);
            var deletedCount = await _context.Offices.ExecuteDeleteAsync(ct);

            _logger.LogInformation("Удалено {OldCount} старых записей офисов", deletedCount);

            _logger.LogInformation("Сохранение новых терминалов...");
            _context.Offices.AddRange(offices);
            await _context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            _logger.LogInformation("Сохранено {NewCount} новых терминалов", offices.Count);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
