using TestTask.Domain.Entities;

namespace TestTask.Domain.Repositories;

public interface IOfficeRepository
{
    Task Delete(CancellationToken ct);
    Task Save(List<Office> offices, CancellationToken ct);
    Task DeleteAndInsert(List<Office> offices, CancellationToken ct);
}
