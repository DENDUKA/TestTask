using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using task.Data;
using task.Entities;
using task.Entities.Dto;

namespace task.Services;

public class ImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly DellinDictionaryDbContext _context;

    public ImportService(ILogger<ImportService> logger, DellinDictionaryDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task ImportAsync(CancellationToken ct = default)
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "terminals.json");
            if (!File.Exists(filePath))
            {
                _logger.LogError("Файл не найден: {FilePath}", filePath);
                return;
            }

            _logger.LogInformation("Чтение файла {FilePath}...", filePath);
            var json = await File.ReadAllTextAsync(filePath, ct);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<RootDto>(json, options);
            if (root?.City == null)
            {
                _logger.LogWarning("JSON пуст или имеет неверную структуру.");
                return;
            }

            var offices = new List<Office>();
            var allPhones = new List<Phone>();

            foreach (var city in root.City)
            {
                if (city.Terminals?.Terminal == null) continue;

                foreach (var t in city.Terminals.Terminal)
                {
                    var office = new Office
                    {
                        Code = t.Id,
                        CityCode = city.Id ?? 0,
                        Uuid = Guid.NewGuid().ToString(), // В JSON нет UUID, генерируем
                        Type = t.IsPVZ ? OfficeType.PVZ : OfficeType.WAREHOUSE,
                        CountryCode = "RU", // Предполагаем RU
                        Coordinates = new Coordinates
                        {
                            Latitude = double.TryParse(t.Latitude, out var lat) ? lat : 0,
                            Longitude = double.TryParse(t.Longitude, out var lon) ? lon : 0
                        },
                        AddressCity = city.Name,
                        AddressStreet = t.Address,
                        WorkTime = t.WorkTime
                    };

                    offices.Add(office);

                    if (t.Phones != null)
                    {
                        foreach (var pDto in t.Phones)
                        {
                            allPhones.Add(new Phone
                            {
                                PhoneNumber = pDto.Number,
                                Additional = pDto.Comment,
                                Office = office
                            });
                        }
                    }
                }
            }

            _logger.LogInformation("Загружено {Count} терминалов из JSON", offices.Count);

            // Очистка БД
            _logger.LogInformation("Очистка старых записей...");
            var oldOfficesCount = await _context.Offices.CountAsync(ct);
            
            // Используем ExecuteDeleteAsync для эффективности (EF Core 7+)
            await _context.Offices.ExecuteDeleteAsync(ct);
            // Удалять телефоны отдельно не нужно, если настроено каскадное удаление в БД, 
            // но ExecuteDelete не вызывает триггеры и каскады на стороне EF. 
            // Поэтому, если в БД есть FK c ON DELETE CASCADE, то телефоны удалятся.
            // Если нет, то нужно удалить и их. Проверим конфигурацию.
            // В DbContext: .OnDelete(DeleteBehavior.Cascade); - это для миграций создаст FK с каскадом.
            // Но чтобы быть уверенным, удалим и телефоны явно, или проверим создание FK.
            // Безопаснее удалить явно, если мы не уверены в состоянии БД.
            // Однако, ExecuteDeleteAsync очищает таблицу. Если есть FK Constraint, удаление Offices упадет.
            // Сначала удалим Phones.
            await _context.Phones.ExecuteDeleteAsync(ct);
            await _context.Offices.ExecuteDeleteAsync(ct);

            _logger.LogInformation("Удалено {OldCount} старых записей", oldOfficesCount);

            // Сохранение новых данных
            _logger.LogInformation("Сохранение новых терминалов...");
            
            // Используем BulkInsert для вставки
            // Важно: BulkInsert требует явной обработки связей, если мы вставляем граф объектов.
            // По умолчанию BulkInsertOrUpdate работает с одной таблицей.
            // Расширение IncludeGraph поддерживается в платной версии или с ограничениями.
            // Для бесплатной версии проще сделать Insert Offices, получить ID, проставить ID в Phones и Insert Phones.
            // Но так как ID автоинкремент (identity), нам нужно, чтобы EF/БД их сгенерировала.
            // Простой вариант: AddRange + SaveChanges - надежно для графов.
            // Если производительность критична (тысячи записей), то можно оптимизировать.
            // Учитывая "Время импорта < 5 минут" и объем JSON (обычно терминалов не миллионы),
            // AddRange вполне справится.
            
            _context.Offices.AddRange(offices);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Сохранено {NewCount} новых терминалов", offices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта: {Message}", ex.Message);
        }
    }
}
