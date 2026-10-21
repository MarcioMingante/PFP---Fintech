using FinTrack.Domain.Transactions;

namespace FinTrack.Infrastructure.Messaging;

public sealed record TransactionCreated(Guid EventId, int Version, Guid TransactionId, Guid UserId,
    decimal Amount, TransactionType Type, DateOnly OccurredOn)
{
    public static TransactionCreated From(Transaction transaction) => new(transaction.Id, 1,
        transaction.Id, transaction.UserId, transaction.Amount, transaction.Type, transaction.OccurredOn);
}
