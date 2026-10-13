namespace FinTrack.Domain.Transactions;

public enum TransactionType { Income = 1, Expense = 2 }

public sealed class Transaction
{
    private Transaction() { }
    public Transaction(Guid userId, Guid accountId, Guid? categoryId, Guid? recurringTransactionId,
        string description, decimal amount, TransactionType type, DateOnly occurredOn)
    {
        if (userId == Guid.Empty || accountId == Guid.Empty || recurringTransactionId == Guid.Empty || categoryId == Guid.Empty)
            throw new ArgumentException("Usuário/conta obrigatórios e referências válidas.");
        var errors = Recurring.RecurringTransaction.Validate(description, amount, type, occurredOn, null);
        if (errors.Count > 0) throw new ArgumentException(errors.First().Value[0], errors.First().Key);
        Id = Guid.NewGuid(); UserId = userId; AccountId = accountId; CategoryId = categoryId;
        RecurringTransactionId = recurringTransactionId; Description = description.Trim();
        Amount = amount; Type = type; OccurredOn = occurredOn;
    }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? RecurringTransactionId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateOnly OccurredOn { get; private set; }
}
