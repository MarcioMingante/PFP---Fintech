using FinTrack.Application.Recurring;
using FinTrack.Domain.Recurring;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class RecurringTransactionRepository(FinTrackDbContext database) : IRecurringTransactionRepository
{
    private const int BatchSizePerRecurrence = 120;
    public async Task AddAsync(RecurringTransaction item, CancellationToken cancellationToken)
    {
        if (item.UserId != database.CurrentUserId) throw new UnauthorizedAccessException();
        database.RecurringTransactions.Add(item);
        await database.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<RecurringTransaction>> ListAsync(CancellationToken cancellationToken)
        => await database.RecurringTransactions.AsNoTracking().OrderBy(item => item.StartDate).ThenBy(item => item.Id).ToListAsync(cancellationToken);
    public Task<RecurringTransaction?> GetAsync(Guid id, CancellationToken cancellationToken)
        => database.RecurringTransactions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private Task LockOwnerAsync(Guid owner, CancellationToken cancellationToken)
    {
        return database.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({owner.ToString()}, 0))", cancellationToken);
    }
    public async Task<RecurringTransaction?> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await LockOwnerAsync(database.CurrentUserId ?? throw new UnauthorizedAccessException(), cancellationToken);
        var item = await database.RecurringTransactions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (item is null) return null;
        item.Deactivate();
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return item;
    }
    public Task<RecurringGenerationResult> GenerateAsync(DateOnly throughDate, CancellationToken cancellationToken)
        => GenerateForOwnerAsync(database.CurrentUserId ?? throw new UnauthorizedAccessException(), throughDate, cancellationToken);

    internal async Task<RecurringGenerationResult> GenerateForOwnerAsync(Guid owner, DateOnly throughDate, CancellationToken cancellationToken)
    {
        if (owner == Guid.Empty) throw new ArgumentException("Proprietário obrigatório.", nameof(owner));
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await LockOwnerAsync(owner, cancellationToken);
        var items = await database.RecurringTransactions.IgnoreQueryFilters().Where(item => item.UserId == owner && item.IsActive && item.NextOccurrenceDate <= throughDate)
            .OrderBy(item => item.Id).ToListAsync(cancellationToken);
        var count = 0;
        foreach (var item in items)
        {
            var generated = 0;
            while (item.IsActive && item.NextOccurrenceDate <= throughDate && generated < BatchSizePerRecurrence)
            {
                database.Transactions.Add(item.GenerateNext());
                generated++; count++;
            }
        }
        await database.SaveChangesAsync(cancellationToken);
        var hasMore = items.Any(item => item.IsActive && item.NextOccurrenceDate <= throughDate);
        await transaction.CommitAsync(cancellationToken);
        return new(throughDate, count, hasMore);
    }
    public async Task<IReadOnlyList<TransactionDto>> ListTransactionsAsync(CancellationToken cancellationToken)
    {
        var items = await database.Transactions.AsNoTracking().OrderByDescending(item => item.OccurredOn)
            .ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return items.Select(TransactionDto.From).ToList();
    }
}
