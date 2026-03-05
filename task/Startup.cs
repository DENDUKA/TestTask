using Microsoft.EntityFrameworkCore;
using TestTask.Infrastructure.Data;
using TestTask.Presentation.Extensions;
using TestTask.Domain.Repositories;
using TestTask.Infrastructure.Repositories;
using TestTask.Application.Services;
using TestTask.Infrastructure.Services;
using TestTask.Presentation;

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
        services.AddSingleton<IDockerService, DockerService>();
        services.AddTransient<IImportService, ImportService>();
        services.AddTransient<AppInitializer>();

        // Add Quartz
        services.AddQuartzServices(Configuration);
    }
}
