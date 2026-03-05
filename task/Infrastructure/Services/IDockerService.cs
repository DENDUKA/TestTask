namespace TestTask.Infrastructure.Services;

public interface IDockerService
{
    Task EnsurePostgresContainerRunning();
}
