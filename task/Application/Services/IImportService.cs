namespace TestTask.Application.Services;

public interface IImportService
{
    Task Import(CancellationToken ct = default);
}
