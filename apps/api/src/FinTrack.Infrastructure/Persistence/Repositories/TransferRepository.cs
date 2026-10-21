using FinTrack.Application.Transfers;
using FinTrack.Domain.Transfers;
using FinTrack.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class TransferRepository(FinTrackDbContext database, TimeProvider clock) : ITransferRepository
{
    public Task<Transfer?> FindAsync(Guid key, CancellationToken cancellationToken) =>
        database.Transfers.AsNoTracking().SingleOrDefaultAsync(item => item.IdempotencyKey == key, cancellationToken);
    public async Task<Transfer?> AddOrGetAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        if (database.CurrentUserId != transfer.UserId) throw new UnauthorizedAccessException();
        database.Transfers.Add(transfer);
        try { await database.SaveChangesAsync(cancellationToken); return transfer; }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            database.Entry(transfer).State = EntityState.Detached;
            return await FindAsync(transfer.IdempotencyKey, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            database.Entry(transfer).State = EntityState.Detached;
            return null; // Account was removed between ownership check and insert; neither side persisted.
        }
    }
    public async Task<IReadOnlyList<TransferDto>> ListAsync(CancellationToken cancellationToken) =>
        (await database.Transfers.AsNoTracking().OrderByDescending(item => item.OccurredOn).ThenBy(item => item.Id).ToListAsync(cancellationToken))
        .Select(TransferDto.From).ToList();
    public async Task<IReadOnlyList<AccountBalanceDto>> BalancesAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        return
        // One SQL statement/snapshot; no mutable balance counter or lost-update window.
        await database.Accounts.AsNoTracking().OrderBy(account => account.Name).Select(account => new AccountBalanceDto(
            account.Id, account.Name,
            (database.Transactions.Where(item => item.AccountId == account.Id && item.OccurredOn <= today && item.Type == TransactionType.Income).Sum(item => (decimal?)item.Amount) ?? 0m)
            - (database.Transactions.Where(item => item.AccountId == account.Id && item.OccurredOn <= today && item.Type == TransactionType.Expense).Sum(item => (decimal?)item.Amount) ?? 0m)
            + (database.Transfers.Where(item => item.DestinationAccountId == account.Id && item.OccurredOn <= today).Sum(item => (decimal?)item.Amount) ?? 0m)
            - (database.Transfers.Where(item => item.SourceAccountId == account.Id && item.OccurredOn <= today).Sum(item => (decimal?)item.Amount) ?? 0m)))
            .ToListAsync(cancellationToken);
    }
}
