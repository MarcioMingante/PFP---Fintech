using FinTrack.Application.Transactions;
using FinTrack.Domain.Transactions;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository(FinTrackDbContext database) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.UserId != database.CurrentUserId) throw new UnauthorizedAccessException();
        database.Transactions.Add(transaction);
        await database.SaveChangesAsync(cancellationToken);
    }
}
