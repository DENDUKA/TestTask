using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;

namespace task.Services;

public class DockerService
{
    private readonly ILogger<DockerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly DockerClient _client;

    public DockerService(ILogger<DockerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Определяем URI Docker в зависимости от ОС
        var dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
            
        _client = new DockerClientConfiguration(dockerUri).CreateClient();
    }

    public async Task EnsurePostgresContainerRunningAsync()
    {
        var settings = _configuration.GetSection("DockerSettings");
        var containerName = settings["ContainerName"] ?? "postgres-db";
        var image = settings["Image"] ?? "postgres:latest";
        var port = settings.GetValue<int>("Port", 5432);
        var password = settings["Password"] ?? "postgres";
        var hostPort = port.ToString();

        _logger.LogInformation("Проверка Docker контейнера '{ContainerName}'...", containerName);

        try
        {
            // Проверка, запущен ли Docker Engine
            await _client.System.PingAsync();
        }
        catch (Exception)
        {
            _logger.LogWarning("Docker Engine не отвечает. Проверьте, запущен ли Docker Desktop. Пропускаем инициализацию контейнера.");
            return;
        }

        try
        {
            // Ищем существующий контейнер
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var existingContainer = containers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));

            if (existingContainer == null)
            {
                _logger.LogInformation("Контейнер '{ContainerName}' не найден. Создание нового...", containerName);
                
                // Проверяем наличие образа, если нет - скачиваем
                var images = await _client.Images.ListImagesAsync(new ImagesListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        { "reference", new Dictionary<string, bool> { { image, true } } }
                    }
                });
                if (images.Count == 0)
                {
                    _logger.LogInformation("Образ '{Image}' не найден. Скачивание...", image);
                    await _client.Images.CreateImageAsync(
                        new ImagesCreateParameters { FromImage = image, Tag = "latest" },
                        null,
                        new Progress<JSONMessage>());
                }

                // Создаем контейнер
                var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = image,
                    Name = containerName,
                    Env = new List<string> { $"POSTGRES_PASSWORD={password}" },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "5432/tcp", new List<PortBinding> { new PortBinding { HostPort = hostPort } } }
                        }
                    }
                });

                _logger.LogInformation("Контейнер создан с ID: {Id}", response.ID);
                
                // Запускаем контейнер
                await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                _logger.LogInformation("Контейнер '{ContainerName}' запущен.", containerName);
                
                // Даем время на инициализацию Postgres
                _logger.LogInformation("Ожидание инициализации PostgreSQL...");
                await Task.Delay(5000);
            }
            else
            {
                if (existingContainer.State != "running")
                {
                    _logger.LogInformation("Контейнер '{ContainerName}' найден, но остановлен. Запуск...", containerName);
                    await _client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                    _logger.LogInformation("Контейнер '{ContainerName}' запущен.", containerName);
                    
                    // Даем время на инициализацию Postgres
                    _logger.LogInformation("Ожидание инициализации PostgreSQL...");
                    await Task.Delay(5000);
                }
                else
                {
                    _logger.LogInformation("Контейнер '{ContainerName}' уже запущен.", containerName);
                    await Task.Delay(1000); // Небольшая задержка, если уже запущен
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при работе с Docker. Убедитесь, что Docker Desktop запущен и доступен.");
        }
    }
}
