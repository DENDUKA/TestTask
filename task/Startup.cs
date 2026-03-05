using Microsoft.EntityFrameworkCore;
using Quartz;
using task.Data;
using task.Jobs;
using task.Services;

namespace task;

public class Startup(IConfiguration configuration)
{
    private const string DefaultConnectionName = "DefaultConnection";

    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        // Add DbContext
        services.AddDbContext<DellinDictionaryDbContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString(DefaultConnectionName)));

        // Add Services
        services.AddSingleton<DockerService>();
        services.AddTransient<ImportService>();
        services.AddTransient<AppInitializer>();

        // Add Quartz
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
            var cronSchedule = Configuration["CronSettings:Schedule"] ?? "0 2 * * *";
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ImportJob-trigger-cron")
                .WithCronSchedule(cronSchedule));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}
