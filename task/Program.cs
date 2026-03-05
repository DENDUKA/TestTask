using task;
using task.Services;

var builder = Host.CreateApplicationBuilder(args);

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
