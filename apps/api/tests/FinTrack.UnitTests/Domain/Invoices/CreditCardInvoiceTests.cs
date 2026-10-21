using FinTrack.Domain.CreditCards;
using FinTrack.Domain.Invoices;
using FinTrack.Domain.Purchases;

namespace FinTrack.UnitTests.Domain.Invoices;

public sealed class CreditCardInvoiceTests
{
    [Fact]
    public void Invoice_RequiresCardOwnerAndValidDates()
    {
        var cardId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var invoice = new CreditCardInvoice(cardId, userId, new(2026, 1, 25), new(2026, 2, 5));
        Assert.NotEqual(Guid.Empty, invoice.Id);
        Assert.Equal(cardId, invoice.CreditCardId);
        Assert.Equal(userId, invoice.UserId);
        Assert.Throws<ArgumentException>(() => new CreditCardInvoice(Guid.Empty, userId, new(2026, 1, 25), new(2026, 2, 5)));
        Assert.Throws<ArgumentException>(() => new CreditCardInvoice(cardId, Guid.Empty, new(2026, 1, 25), new(2026, 2, 5)));
        Assert.Throws<ArgumentException>(() => new CreditCardInvoice(cardId, userId, default, new(2026, 2, 5)));
        Assert.Throws<ArgumentException>(() => new CreditCardInvoice(cardId, userId, new(2026, 1, 25), new(2026, 1, 25)));
        Assert.Throws<ArgumentException>(() => new CreditCardInvoice(cardId, userId, new(2026, 1, 25), new(2026, 1, 24)));
    }

    [Fact]
    public void Installment_RequiresMatchingInvoiceAndCannotBeMoved()
    {
        var card = new CreditCard("Cartão", 5000m, 25, 5);
        card.AssignOwner(Guid.NewGuid());
        var item = new Purchase(card, "Compra", 100m, 1, new(2026, 1, 1), Guid.NewGuid()).Installments.Single();
        var invoice = new CreditCardInvoice(card.Id, card.UserId, item.ClosingDate, item.DueDate);
        Assert.Throws<ArgumentException>(() => item.AssignInvoice(invoice, Guid.NewGuid(), card.UserId));
        Assert.Throws<ArgumentException>(() => item.AssignInvoice(invoice, card.Id, Guid.NewGuid()));
        var wrongCycle = new CreditCardInvoice(card.Id, card.UserId, item.ClosingDate.AddMonths(1), item.DueDate.AddMonths(1));
        Assert.Throws<ArgumentException>(() => item.AssignInvoice(wrongCycle, card.Id, card.UserId));
        item.AssignInvoice(invoice, card.Id, card.UserId);
        item.AssignInvoice(invoice, card.Id, card.UserId);
        Assert.Equal(invoice.Id, item.InvoiceId);
        var duplicate = new CreditCardInvoice(card.Id, card.UserId, item.ClosingDate, item.DueDate);
        Assert.Throws<InvalidOperationException>(() => item.AssignInvoice(duplicate, card.Id, card.UserId));
    }
}
