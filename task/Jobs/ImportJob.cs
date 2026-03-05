using Quartz;
using TestTask.Services;

namespace TestTask.Jobs;

[DisallowConcurrentExecution]
public class ImportJob(ILogger<ImportJob> logger, ImportService importService) : IJob
{
    private readonly ILogger<ImportJob> _logger = logger;
    private readonly ImportService _importService = importService;

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Запуск Quartz Job: {JobKey} в {Time}", context.JobDetail.Key, DateTimeOffset.Now);

        try
        {
            await _importService.Import(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка выполнения Quartz Job");
            // В зависимости от требований, можно выбросить JobExecutionException
            // throw new JobExecutionException(ex);
        }

        _logger.LogInformation("Завершение Quartz Job: {JobKey} в {Time}", context.JobDetail.Key, DateTimeOffset.Now);
    }
}
