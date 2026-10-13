namespace FinTrack.Domain.Invoices;

public sealed class CreditCardInvoice
{
    private CreditCardInvoice() { }
    public CreditCardInvoice(Guid creditCardId, Guid userId, DateOnly closingDate, DateOnly dueDate)
    {
        if (creditCardId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Cartão e proprietário obrigatórios.");
        if (closingDate == default || dueDate <= closingDate)
            throw new ArgumentException("Vencimento deve ser posterior ao fechamento.", nameof(dueDate));
        Id = Guid.NewGuid(); CreditCardId = creditCardId; UserId = userId;
        ClosingDate = closingDate; DueDate = dueDate;
    }
    public Guid Id { get; private set; }
    public Guid CreditCardId { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly ClosingDate { get; private set; }
    public DateOnly DueDate { get; private set; }
}
