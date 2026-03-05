using Cronos;
using task.Services;

namespace task;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly CronExpression _cronExpression;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;

        var schedule = _configuration["CronSettings:Schedule"] ?? "0 2 * * *"; // Default: 02:00 daily
        try 
        {
            _cronExpression = CronExpression.Parse(schedule);
            _logger.LogInformation("Запуск с расписанием: {Schedule}", schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга Cron выражения: {Schedule}. Используется значение по умолчанию '0 2 * * *'", schedule);
            _cronExpression = CronExpression.Parse("0 2 * * *");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Первоначальный запуск импорта при старте приложения...");
        await RunImportAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var utcNow = DateTime.UtcNow;
            var nextUtc = _cronExpression.GetNextOccurrence(utcNow);

            if (nextUtc.HasValue)
            {
                var delay = nextUtc.Value - utcNow;
                _logger.LogInformation("Следующий запуск запланирован на: {NextRun} (через {Delay})", nextUtc.Value.ToLocalTime(), delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RunImportAsync(stoppingToken);
                }
            }
            else
            {
                _logger.LogWarning("Не удалось определить время следующего запуска. Остановка воркера.");
                break;
            }
        }
    }

    private async Task RunImportAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Начало выполнения задачи импорта: {Time}", DateTimeOffset.Now);
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
            await importService.ImportAsync(stoppingToken);
        }

        _logger.LogInformation("Завершение задачи импорта: {Time}", DateTimeOffset.Now);
    }
}
