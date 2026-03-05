using Microsoft.EntityFrameworkCore;
using TestTask.Domain.Entities;
using TestTask.Infrastructure.Data;
using TestTask.Domain.Repositories;

namespace TestTask.Infrastructure.Repositories;public class OfficeRepository(DellinDictionaryDbContext context, ILogger<OfficeRepository> logger) : IOfficeRepository
{
    private readonly DellinDictionaryDbContext _context = context;
    private readonly ILogger<OfficeRepository> _logger = logger;

    public async Task Update(List<Office> offices, CancellationToken ct)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await Delete(ct);
            await Save(offices, ct);

            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            _logger.LogError(ex, "Ошибка при обновлении записей");

            throw;
        }
    }

    public async Task Delete(CancellationToken ct)
    {
        await _context.Phones.ExecuteDeleteAsync(ct);
        var deletedCount = await _context.Offices.ExecuteDeleteAsync(ct);

        _logger.LogInformation("Удалено {OldCount} старых записей", deletedCount);
    }

    public async Task Save(List<Office> offices, CancellationToken ct)
    {
        _context.Offices.AddRange(offices);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Сохранено {NewCount} новых терминалов", offices.Count);
    }
}
