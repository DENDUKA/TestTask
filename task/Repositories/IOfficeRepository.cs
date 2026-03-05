using TestTask.Entities;

namespace TestTask.Repositories;

public interface IOfficeRepository
{
    Task Delete(CancellationToken ct);
    Task Save(List<Office> offices, CancellationToken ct);
    Task Update(List<Office> offices, CancellationToken ct);
}
