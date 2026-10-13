namespace FinTrack.Domain.Purchases;

public sealed class Installment
{
    private Installment() { }
    internal Installment(Guid purchaseId, int number, decimal amount, DateOnly closingDate, DateOnly dueDate)
    {
        Id = Guid.NewGuid(); PurchaseId = purchaseId; Number = number;
        Amount = amount; ClosingDate = closingDate; DueDate = dueDate;
    }
    public Guid Id { get; private set; }
    public Guid PurchaseId { get; private set; }
    public int Number { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly ClosingDate { get; private set; }
    public DateOnly DueDate { get; private set; }
}
