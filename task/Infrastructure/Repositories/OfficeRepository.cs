using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using TestTask.Domain.Entities;
using TestTask.Infrastructure.Data;
using TestTask.Domain.Repositories;

namespace TestTask.Infrastructure.Repositories;

public class OfficeRepository(DellinDictionaryDbContext context, ILogger<OfficeRepository> logger) : IOfficeRepository
{
    private const int BatchSize = 1000;

    private readonly DellinDictionaryDbContext _context = context;
    private readonly ILogger<OfficeRepository> _logger = logger;

    private async Task DeleteAll(CancellationToken ct)
    {
        await _context.Phones.ExecuteDeleteAsync(ct);
        var deletedCount = await _context.Offices.ExecuteDeleteAsync(ct);

        _logger.LogInformation("Удалено {OldCount} старых записей", deletedCount);
    }

    private async Task BulkInsert(List<Office> offices, CancellationToken ct)
    {
        if (offices.Count == 0) return;

        // Используем стандартный EF Core AddRange, так как BulkExtensions имеет проблемы с Owned Types + Graph + OutputIdentity
        // EF Core автоматически пакетирует вставки, что достаточно эффективно для данного объема
        await _context.Offices.AddRangeAsync(offices, ct);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Сохранено {NewCount} новых терминалов", offices.Count);
    }

    public async Task ReplaceAll(List<Office> offices, CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                // Удаляем все записи
                await DeleteAll(ct);

                // Вставляем новые записи
                await BulkInsert(offices, ct);

                await transaction.CommitAsync(ct);
                _logger.LogInformation("Полная замена данных завершена. Всего записей: {Count}", offices.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Ошибка при полной замене данных");
                throw;
            }
        });
    }
}
