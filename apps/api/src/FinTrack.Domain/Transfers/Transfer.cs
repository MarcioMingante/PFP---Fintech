using FinTrack.Domain.Recurring;
using FinTrack.Domain.Transactions;

namespace FinTrack.Domain.Transfers;

// One ledger entry represents both sides; a partially persisted transfer cannot exist.
public sealed class Transfer
{
    private Transfer() { }
    public Transfer(Guid userId, Guid sourceAccountId, Guid destinationAccountId,
        string description, decimal amount, DateOnly occurredOn, Guid idempotencyKey)
    {
        var errors = Validate(sourceAccountId, destinationAccountId, description, amount, occurredOn, idempotencyKey);
        if (userId == Guid.Empty || errors.Count > 0) throw new ArgumentException("Transferência inválida.");
        Id = Guid.NewGuid(); UserId = userId; SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId; Description = description.Trim();
        Amount = amount; OccurredOn = occurredOn; IdempotencyKey = idempotencyKey;
    }
    public static Dictionary<string, string[]> Validate(Guid source, Guid destination, string description,
        decimal amount, DateOnly date, Guid key)
    {
        var errors = RecurringTransaction.Validate(description, amount, TransactionType.Expense, date, null);
        if (errors.Remove("startDate", out var dates)) errors["occurredOn"] = dates;
        if (source == Guid.Empty) errors["sourceAccountId"] = ["Conta de origem obrigatória."];
        if (destination == Guid.Empty || destination == source) errors["destinationAccountId"] = ["Escolha uma conta de destino diferente da origem."];
        if (key == Guid.Empty) errors["idempotencyKey"] = ["Chave de idempotência obrigatória."];
        return errors;
    }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SourceAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public string Description { get; private set; } = "";
    public decimal Amount { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public Guid IdempotencyKey { get; private set; }
}
