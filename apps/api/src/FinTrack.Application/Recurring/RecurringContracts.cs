using FinTrack.Domain.Recurring;
using FinTrack.Domain.Transactions;

namespace FinTrack.Application.Recurring;

public sealed record CreateRecurringTransactionCommand(Guid AccountId, Guid? CategoryId, string Description,
    decimal Amount, string Type, DateOnly StartDate, DateOnly? EndDate);
public sealed record GenerateRecurringTransactionsRequest(DateOnly? ThroughDate);
public sealed record RecurringTransactionDto(Guid Id, Guid AccountId, Guid? CategoryId, string Description,
    decimal Amount, string Type, string Frequency, DateOnly StartDate, DateOnly? EndDate,
    DateOnly? NextOccurrenceDate, bool IsActive)
{
    public static RecurringTransactionDto From(RecurringTransaction item) => new(item.Id, item.AccountId,
        item.CategoryId, item.Description, item.Amount, item.Type.ToString(), "Monthly", item.StartDate,
        item.EndDate, item.NextOccurrenceDate, item.IsActive);
}
public sealed record TransactionDto(Guid Id, Guid AccountId, Guid? CategoryId, Guid RecurringTransactionId,
    string Description, decimal Amount, string Type, DateOnly OccurredOn)
{
    public static TransactionDto From(Transaction item) => new(item.Id, item.AccountId, item.CategoryId,
        item.RecurringTransactionId, item.Description, item.Amount, item.Type.ToString(), item.OccurredOn);
}
public sealed record RecurringGenerationResult(DateOnly ThroughDate, int GeneratedCount, bool HasMore);
