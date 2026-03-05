using System.Net.Sockets;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace task.Services;

public class DockerService
{
    private const string ConfigSection = "DockerSettings";
    private const string ContainerNameKey = "ContainerName";
    private const string ImageKey = "Image";
    private const string PortKey = "Port";
    private const string PasswordKey = "Password";

    private const string DefaultContainerName = "postgres-db";
    private const string DefaultImage = "postgres:latest";
    private const int DefaultPort = 5432;
    private const string DefaultPassword = "postgres";
    
    private const string DockerPipeWindows = "npipe://./pipe/docker_engine";
    private const string PostgresPortBinding = "5432/tcp";
    private const string ContainerStateRunning = "running";
    private const string ImageFilterReference = "reference";
    private const string ImageTagLatest = "latest";
    private const string Localhost = "localhost";
    private const int WaitForPostgresTimeoutSeconds = 30;
    private const int WaitForPostgresRetryDelayMilliseconds = 500;

    private readonly ILogger<DockerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly DockerClient _client;

    public DockerService(ILogger<DockerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var dockerUri = new Uri(DockerPipeWindows);
            
        _client = new DockerClientConfiguration(dockerUri).CreateClient();
    }

    public async Task EnsurePostgresContainerRunningAsync()
    {
        var settings = _configuration.GetSection(ConfigSection);
        var containerName = settings[ContainerNameKey] ?? DefaultContainerName;
        var image = settings[ImageKey] ?? DefaultImage;
        var port = settings.GetValue(PortKey, DefaultPort);
        var password = settings[PasswordKey] ?? DefaultPassword;
        var hostPort = port.ToString();

        _logger.LogInformation("Проверка Docker контейнера '{ContainerName}'...", containerName);

        try
        {
            await _client.System.PingAsync();
        }
        catch (Exception)
        {
            _logger.LogWarning("Docker Engine не отвечает. Проверьте, запущен ли Docker Desktop. Пропускаем инициализацию контейнера.");
            return;
        }

        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            var existingContainer = containers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));

            if (existingContainer == null)
            {
                await CreateAndStartContainerAsync(containerName, image, password, hostPort);
            }
            else if (existingContainer.State != ContainerStateRunning)
            {
                _logger.LogInformation("Контейнер '{ContainerName}' найден, но остановлен. Запуск...", containerName);
                await _client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                _logger.LogInformation("Контейнер '{ContainerName}' запущен.", containerName);
            }
            else
            {
                _logger.LogInformation("Контейнер '{ContainerName}' уже запущен.", containerName);
            }

            await WaitForPostgresAsync(Localhost, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при работе с Docker. Убедитесь, что Docker Desktop запущен и доступен.");
        }
    }

    private async Task CreateAndStartContainerAsync(string containerName, string image, string password, string hostPort)
    {
        _logger.LogInformation("Контейнер '{ContainerName}' не найден. Создание нового...", containerName);

        // Проверяем наличие образа
        var images = await _client.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { ImageFilterReference, new Dictionary<string, bool> { { image, true } } }
            }
        });

        if (images.Count == 0)
        {
            _logger.LogInformation("Образ '{Image}' не найден. Скачивание...", image);
            await _client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image, Tag = ImageTagLatest },
                null,
                new Progress<JSONMessage>());
        }

        var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = image,
            Name = containerName,
            Env = new List<string> { $"POSTGRES_PASSWORD={password}" },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { PostgresPortBinding, new List<PortBinding> { new PortBinding { HostPort = hostPort } } }
                }
            }
        });

        _logger.LogInformation("Контейнер создан с ID: {Id}", response.ID);
        await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        _logger.LogInformation("Контейнер '{ContainerName}' запущен.", containerName);
    }

    private async Task WaitForPostgresAsync(string host, int port)
    {
        _logger.LogInformation("Ожидание готовности PostgreSQL на порту {Port}...", port);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(WaitForPostgresTimeoutSeconds));
        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port, cts.Token);
                _logger.LogInformation("PostgreSQL доступен.");
                return;
            }
            catch
            {
                await Task.Delay(WaitForPostgresRetryDelayMilliseconds, cts.Token);
            }
        }
        _logger.LogWarning("Не удалось подключиться к PostgreSQL за отведенное время.");
    }
}
