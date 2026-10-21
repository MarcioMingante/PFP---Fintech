namespace FinTrack.Domain.Purchases;

public sealed class Installment
{
    private Installment() { }
    internal Installment(Guid purchaseId, int number, decimal amount, DateOnly closingDate, DateOnly dueDate)
    {
        Id = Guid.NewGuid(); PurchaseId = purchaseId; Number = number;
        Amount = amount; ClosingDate = closingDate; DueDate = dueDate;
    }
    public void AssignInvoice(FinTrack.Domain.Invoices.CreditCardInvoice invoice, Guid creditCardId, Guid userId)
    {
        if (invoice.CreditCardId != creditCardId || invoice.UserId != userId
            || invoice.ClosingDate != ClosingDate || invoice.DueDate != DueDate)
            throw new ArgumentException("Fatura incompatível com a parcela.", nameof(invoice));
        if (InvoiceId != Guid.Empty && InvoiceId != invoice.Id)
            throw new InvalidOperationException("Fatura da parcela não pode ser alterada.");
        InvoiceId = invoice.Id;
    }
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid PurchaseId { get; private set; }
    public int Number { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly ClosingDate { get; private set; }
    public DateOnly DueDate { get; private set; }
}
