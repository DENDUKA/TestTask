using TestTask.Domain.Entities;

namespace TestTask.Domain.Repositories;

public interface IOfficeRepository
{
    Task ReplaceAll(List<Office> offices, CancellationToken ct);
}
