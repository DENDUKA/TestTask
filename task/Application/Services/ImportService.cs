using System.Text.Json;
using System.Globalization;
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

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

            _logger.LogInformation("Начало импорта из файла {FilePath}...", filePath);

            using var fileStream = File.OpenRead(filePath);
            var root = await JsonSerializer.DeserializeAsync<RootDto>(fileStream, _jsonOptions, ct);

            if (root?.City == null)
            {
                _logger.LogWarning("Файл пуст или имеет неверный формат.");
                return;
            }

            var allOffices = new List<Office>();
            foreach (var city in root.City)
            {
                if (city.Terminals?.Terminal != null)
                {
                    foreach (var t in city.Terminals.Terminal)
                    {
                        var office = MapToOffice(t, city);
                        allOffices.Add(office);
                    }
                }
            }

            if (allOffices.Count > 0)
            {
                await _repository.ReplaceAll(allOffices, ct);
            }
            else
            {
                _logger.LogWarning("Нет данных для импорта.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта: {Exception}", ex.Message);
        }
    }

    internal static Office MapToOffice(TerminalDto t, CityDto city)
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
                Latitude = double.TryParse(t.Latitude, CultureInfo.InvariantCulture, out var lat) ? lat : 0,
                Longitude = double.TryParse(t.Longitude, CultureInfo.InvariantCulture, out var lon) ? lon : 0
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
