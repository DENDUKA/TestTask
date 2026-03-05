using Microsoft.EntityFrameworkCore;
using Quartz;
using task.Data;
using task.Extensions;
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
        services.AddQuartzServices(Configuration);
    }
}
