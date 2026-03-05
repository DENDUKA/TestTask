using System.Text.Json;
using TestTask.Domain.Entities;
using TestTask.Application.DTOs;
using TestTask.Domain.Repositories;

namespace TestTask.Application.Services;

public class ImportService(ILogger<ImportService> logger, IOfficeRepository repository) : IImportService
{
    private const string FilesDirectory = "Infrastructure/Files";
    private const string TerminalsFile = "terminals.json";
    private const string DefaultCountryCode = "RU";

    private readonly ILogger<ImportService> _logger = logger;
    private readonly IOfficeRepository _repository = repository;

    public async Task Import(CancellationToken ct = default)
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

            await _repository.Update(offices, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта: {Exception}", ex.Message);
        }
    }

    private static Office MapToOffice(TerminalDto t, CityDto city)
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
}
