using FinTrack.Domain.Recurring;

namespace FinTrack.Application.Recurring;

public interface IRecurringTransactionRepository
{
    Task AddAsync(RecurringTransaction item, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecurringTransaction>> ListAsync(CancellationToken cancellationToken);
    Task<RecurringTransaction?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<RecurringTransaction?> DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<RecurringGenerationResult> GenerateAsync(DateOnly throughDate, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionDto>> ListTransactionsAsync(CancellationToken cancellationToken);
}
