using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using task;
using task.Logging;
using task.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.FormatterName = "custom");
builder.Logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<AppInitializer>();
    await initializer.InitializeAsync();
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
