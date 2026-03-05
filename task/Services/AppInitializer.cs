using Npgsql;
using task.Data;

namespace task.Services;

public class AppInitializer(
    ILogger<AppInitializer> logger,
    IConfiguration configuration,
    DockerService dockerService,
    IServiceProvider serviceProvider)
{
    private const string DefaultConnection = "DefaultConnection";
    private const string PostgresDbName = "postgres";
    private const string CheckDbExistsQuery = "SELECT 1 FROM pg_database WHERE datname = '{0}'";
    private const string CreateDbCommand = "CREATE DATABASE \"{0}\"";

    private readonly ILogger<AppInitializer> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly DockerService _dockerService = dockerService;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Запуск инициализации приложения...");

        try
        {
            await _dockerService.EnsurePostgresContainerRunningAsync();
            _logger.LogInformation("Docker проверен.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка Docker: {Message}", ex.Message);
            // Продолжаем, возможно БД внешняя
        }

        await EnsureDatabaseExistsAsync();
        await ApplyMigrationsAsync();

        _logger.LogInformation("Инициализация завершена.");
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        var connectionString = _configuration.GetConnectionString(DefaultConnection);
        try
        {
            var builderStr = new NpgsqlConnectionStringBuilder(connectionString);
            var dbName = builderStr.Database;
            builderStr.Database = PostgresDbName; // Подключаемся к postgres для создания БД
            
            using var conn = new NpgsqlConnection(builderStr.ConnectionString);
            await conn.OpenAsync();
            
            var existsCmd = conn.CreateCommand();
            existsCmd.CommandText = string.Format(CheckDbExistsQuery, dbName);
            var exists = await existsCmd.ExecuteScalarAsync();
            
            if (exists == null)
            {
                _logger.LogInformation("База данных {DbName} не найдена. Создание...", dbName);
                var createCmd = conn.CreateCommand();
                createCmd.CommandText = string.Format(CreateDbCommand, dbName);
                await createCmd.ExecuteNonQueryAsync();
                _logger.LogInformation("База данных {DbName} создана.", dbName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке/создании БД: {Message}", ex.Message);
        }
    }

    private async Task ApplyMigrationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DellinDictionaryDbContext>();
        try
        {
            await db.Database.EnsureCreatedAsync();
            _logger.LogInformation("База данных успешно инициализирована (схема создана).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при инициализации схемы БД: {Message}", ex.Message);
        }
    }
}
