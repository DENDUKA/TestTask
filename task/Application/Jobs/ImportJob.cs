using Quartz;
using TestTask.Application.Services;

namespace TestTask.Application.Jobs;

[DisallowConcurrentExecution]
public class ImportJob(ILogger<ImportJob> logger, IImportService importService) : IJob
{
    private readonly ILogger<ImportJob> _logger = logger;
    private readonly IImportService _importService = importService;

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
