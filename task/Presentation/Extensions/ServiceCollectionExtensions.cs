using Quartz;
using TestTask.Application.Jobs;

namespace TestTask.Presentation.Extensions;

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
                TimeZoneInfo timeZone;
                try
                {
                    // For Linux/Docker
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                }
                catch
                {
                    try
                    {
                        // For Windows
                        timeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                    }
                    catch
                    {
                        // Fallback to Local
                        timeZone = TimeZoneInfo.Local;
                    }
                }

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("ImportJob-trigger-cron")
                    .WithCronSchedule(cronSchedule, x => x.InTimeZone(timeZone)));
            }
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}
