using FinTrack.Domain.CreditCards;

namespace FinTrack.Domain.Purchases;

public sealed class Purchase
{
    private readonly List<Installment> installments = [];
    private Purchase() { }
    public Purchase(CreditCard card, string description, decimal amount, int installmentCount,
        DateOnly purchaseDate, Guid idempotencyKey)
    {
        var errors = Validate(description, amount, installmentCount, purchaseDate, idempotencyKey);
        if (errors.Count > 0) throw new ArgumentException(errors.First().Value[0], errors.First().Key);
        if (card.UserId == Guid.Empty) throw new ArgumentException("Cartão precisa de proprietário.", nameof(card));
        Id = Guid.NewGuid(); CreditCardId = card.Id; UserId = card.UserId;
        Description = description.Trim(); Amount = amount; InstallmentCount = installmentCount;
        PurchaseDate = purchaseDate; IdempotencyKey = idempotencyKey;
        ClosingDay = card.ClosingDay; DueDay = card.DueDay;
        installments.AddRange(InstallmentSchedule.Generate(Id, amount, installmentCount, purchaseDate, ClosingDay, DueDay));
    }
    public Guid Id { get; private set; }
    public Guid CreditCardId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public int InstallmentCount { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public IReadOnlyCollection<Installment> Installments => installments.AsReadOnly();

    public static Dictionary<string, string[]> Validate(string? description, decimal amount,
        int installmentCount, DateOnly purchaseDate, Guid idempotencyKey)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 200)
            errors["description"] = ["Descrição deve conter entre 1 e 200 caracteres."];
        if (amount <= 0 || amount > CreditCardRules.MaximumLimit || decimal.Round(amount, 2) != amount)
            errors["amount"] = ["Valor deve ser positivo, com até 16 dígitos inteiros e duas casas decimais."];
        if (installmentCount is < 1 or > 120 || amount * 100m < installmentCount)
            errors["installmentCount"] = ["Use entre 1 e 120 parcelas, cada uma com pelo menos um centavo."];
        // Leave enough calendar space for all installments and the following due month.
        if (purchaseDate == default || purchaseDate > new DateOnly(9989, 1, 1))
            errors["purchaseDate"] = ["Data deve estar entre 0001-01-02 e 9989-01-01."];
        if (idempotencyKey == Guid.Empty) errors["idempotencyKey"] = ["Chave de idempotência obrigatória."];
        return errors;
    }
}
