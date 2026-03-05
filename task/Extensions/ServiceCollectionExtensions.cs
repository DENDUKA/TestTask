using Quartz;
using task.Jobs;

namespace task.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuartzServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("ImportJob");
            q.AddJob<ImportJob>(opts => opts.WithIdentity(jobKey));

            // Trigger 1: Run immediately on startup
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ImportJob-trigger-now")
                .StartNow());

            // Trigger 2: Run on schedule
            var cronSchedule = configuration["CronSettings:Schedule"];
            if (!string.IsNullOrEmpty(cronSchedule))
            {
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("ImportJob-trigger-cron")
                    .WithCronSchedule(cronSchedule));
            }
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}
