using Microsoft.EntityFrameworkCore;
using task.Data;
using task;
using task.Services;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext
builder.Services.AddDbContext<DellinDictionaryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Services
builder.Services.AddSingleton<DockerService>();
builder.Services.AddTransient<ImportService>();

// Add Hosted Service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Ensure Docker container is running before starting the app
var dockerService = host.Services.GetRequiredService<DockerService>();
Console.WriteLine("Запуск проверки Docker...");
try 
{
    await dockerService.EnsurePostgresContainerRunningAsync();
    Console.WriteLine("Docker проверен.");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка Docker: {ex}");
    File.AppendAllText("error.log", $"Docker Error: {ex}\n");
}

// Ensure Database Exists
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
try
{
    var builderStr = new NpgsqlConnectionStringBuilder(connectionString);
    var dbName = builderStr.Database;
    builderStr.Database = "postgres"; // Подключаемся к postgres для создания БД
    
    using var conn = new NpgsqlConnection(builderStr.ConnectionString);
    await conn.OpenAsync();
    
    var existsCmd = conn.CreateCommand();
    existsCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
    var exists = await existsCmd.ExecuteScalarAsync();
    
    if (exists == null)
    {
        Console.WriteLine($"База данных {dbName} не найдена. Создание...");
        var createCmd = conn.CreateCommand();
        createCmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await createCmd.ExecuteNonQueryAsync();
        Console.WriteLine($"База данных {dbName} создана.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка при проверке/создании БД: {ex.Message}");
    File.AppendAllText("error.log", $"DB Create Error: {ex}\n");
}

// Apply migrations at startup
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DellinDictionaryDbContext>();
    // В тестовом задании используем EnsureCreated для автоматического создания схемы без явных миграций,
    // если EF Tools не работают. Но для продакшена лучше MigrateAsync.
    // Если миграций нет, EnsureCreated создаст таблицы.
    try {
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("База данных успешно инициализирована (схема создана).");
    } catch (Exception ex) {
        Console.WriteLine($"Ошибка при инициализации схемы БД: {ex.Message}");
        File.AppendAllText("error.log", $"DB Schema Init Error: {ex}\n");
        // Не падаем, чтобы логи успели вывестись
    }
    // await db.Database.MigrateAsync();
}

try 
{
    host.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка Host: {ex}");
    File.AppendAllText("error.log", $"Host Error: {ex}\n");
}
