using Microsoft.EntityFrameworkCore;
using TestTask.Data;
using TestTask.Extensions;
using TestTask.Repositories;
using TestTask.Services;

namespace TestTask;

public class Startup(IConfiguration configuration)
{
    private const string DefaultConnectionName = "DefaultConnection";

    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        // Add DbContext
        services.AddDbContext<DellinDictionaryDbContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString(DefaultConnectionName)));

        // Add Repositories
        services.AddScoped<IOfficeRepository, OfficeRepository>();

        // Add Services
        services.AddSingleton<DockerService>();
        services.AddTransient<ImportService>();
        services.AddTransient<AppInitializer>();

        // Add Quartz
        services.AddQuartzServices(Configuration);
    }
}
