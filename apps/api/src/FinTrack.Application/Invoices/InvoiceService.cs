using FinTrack.Application.CreditCards;
using FinTrack.Domain.Purchases;

namespace FinTrack.Application.Invoices;

public sealed class InvoiceService(ICreditCardRepository cards, IInvoiceRepository invoices, TimeProvider clock)
{
    public static Dictionary<string, string[]> ValidateDate(DateOnly? asOf)
    {
        var errors = new Dictionary<string, string[]>();
        if (asOf is { } date && (date == default || date > new DateOnly(9998, 12, 1)))
            errors["asOf"] = ["Data deve estar entre 0001-01-02 e 9998-12-01."];
        return errors;
    }

    public async Task<IReadOnlyList<InvoiceDto>?> ListAsync(Guid cardId, CancellationToken cancellationToken)
    {
        if (await cards.GetByIdAsync(cardId, cancellationToken) is null) return null;
        return await invoices.ListAsync(cardId, cancellationToken);
    }

    public async Task<CreditCardInvoiceSummaryDto?> SummaryAsync(Guid cardId, DateOnly? asOf, CancellationToken cancellationToken)
    {
        var date = asOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (ValidateDate(date).Count > 0) throw new ArgumentOutOfRangeException(nameof(asOf));
        var card = await cards.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return null;
        var all = await invoices.ListAsync(cardId, cancellationToken);
        // The active collection cycle excludes purchases made on the closing date.
        var month = new DateOnly(date.Year, date.Month, 1);
        if (date >= InstallmentSchedule.DayInMonth(month, card.ClosingDay)) month = month.AddMonths(1);
        var closing = InstallmentSchedule.DayInMonth(month, card.ClosingDay);
        var due = InstallmentSchedule.DayInMonth(month, card.DueDay);
        if (due <= closing) due = InstallmentSchedule.DayInMonth(month.AddMonths(1), card.DueDay);
        var upcoming = all.Where(invoice => invoice.ClosingDate > date)
            .OrderBy(invoice => invoice.ClosingDate).ThenBy(invoice => invoice.DueDate).ThenBy(invoice => invoice.Id).ToList();
        // An older calendar snapshot may close before the card's current configuration.
        var current = upcoming.FirstOrDefault(invoice => invoice.ClosingDate <= closing)
            ?? new InvoiceDto(null, cardId, closing, due, 0m, []);
        var future = upcoming.Where(invoice => invoice.Id != current.Id).ToList();
        var used = all.Sum(invoice => invoice.Total);
        return new(cardId, date, card.Limit, used, card.Limit - used, current, future);
    }
}
