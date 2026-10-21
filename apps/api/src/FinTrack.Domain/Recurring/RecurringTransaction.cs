using FinTrack.Domain.Transactions;

namespace FinTrack.Domain.Recurring;

public sealed class RecurringTransaction
{
    public const decimal MaximumAmount = 9999999999999999.99m;
    public static readonly DateOnly MaximumDate = new(9998, 12, 1);
    private RecurringTransaction() { }
    public RecurringTransaction(Guid userId, Guid accountId, Guid? categoryId, string description,
        decimal amount, TransactionType type, DateOnly startDate, DateOnly? endDate)
    {
        if (userId == Guid.Empty || accountId == Guid.Empty || categoryId == Guid.Empty)
            throw new ArgumentException("Usuário/conta obrigatórios e categoria válida.");
        var errors = Validate(description, amount, type, startDate, endDate);
        if (errors.Count > 0) throw new ArgumentException(errors.First().Value[0], errors.First().Key);
        Id = Guid.NewGuid(); UserId = userId; AccountId = accountId; CategoryId = categoryId;
        Description = description.Trim(); Amount = amount; Type = type;
        StartDate = startDate; EndDate = endDate; NextOccurrenceDate = startDate; IsActive = true;
    }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? NextOccurrenceDate { get; private set; }
    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;

    public Transaction GenerateNext()
    {
        if (!IsActive || NextOccurrenceDate is null)
            throw new InvalidOperationException("Recorrência não possui ocorrência ativa.");
        var date = NextOccurrenceDate.Value;
        var transaction = new Transaction(UserId, AccountId, CategoryId, Id, Description, Amount, Type, date);
        var month = new DateOnly(date.Year, date.Month, 1).AddMonths(1);
        var next = new DateOnly(month.Year, month.Month, Math.Min(StartDate.Day, DateTime.DaysInMonth(month.Year, month.Month)));
        NextOccurrenceDate = next > MaximumDate || (EndDate.HasValue && next > EndDate.Value) ? null : next;
        if (NextOccurrenceDate is null) IsActive = false;
        return transaction;
    }

    public static Dictionary<string, string[]> Validate(string? description, decimal amount,
        TransactionType type, DateOnly startDate, DateOnly? endDate)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 200)
            errors["description"] = ["Descrição deve conter entre 1 e 200 caracteres."];
        if (amount <= 0 || amount > MaximumAmount || decimal.Round(amount, 2) != amount)
            errors["amount"] = ["Valor deve ser positivo, com até 16 dígitos inteiros e duas casas decimais."];
        if (type is not (TransactionType.Income or TransactionType.Expense))
            errors["type"] = ["Tipo deve ser Income ou Expense."];
        if (startDate == default || startDate > MaximumDate)
            errors["startDate"] = ["Data inicial deve estar entre 0001-01-02 e 9998-12-01."];
        if (endDate.HasValue && (endDate < startDate || endDate > MaximumDate))
            errors["endDate"] = ["Data final deve ser igual ou posterior à inicial, até 9998-12-01."];
        return errors;
    }
}
