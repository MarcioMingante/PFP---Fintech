using FinTrack.Application.Invoices;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository(FinTrackDbContext database) : IInvoiceRepository
{
    public async Task<IReadOnlyList<InvoiceDto>> ListAsync(Guid creditCardId, CancellationToken cancellationToken)
    {
        // A single SQL statement keeps invoice totals, items and limit inputs consistent.
        var rows = await (
            from invoice in database.CreditCardInvoices.AsNoTracking()
            join installment in database.Set<FinTrack.Domain.Purchases.Installment>() on invoice.Id equals installment.InvoiceId
            join purchase in database.Purchases on installment.PurchaseId equals purchase.Id
            where invoice.CreditCardId == creditCardId
            orderby invoice.ClosingDate, invoice.DueDate, purchase.PurchaseDate, purchase.Id, installment.Number
            select new { invoice.Id, invoice.ClosingDate, invoice.DueDate,
                Item = new InvoiceItemDto(installment.Id, purchase.Id, purchase.Description,
                    installment.Number, purchase.InstallmentCount, installment.Amount) }
        ).ToListAsync(cancellationToken);
        return rows.GroupBy(row => new { row.Id, row.ClosingDate, row.DueDate })
            .Select(group => new InvoiceDto(group.Key.Id, creditCardId, group.Key.ClosingDate, group.Key.DueDate,
                group.Sum(row => row.Item.Amount), group.Select(row => row.Item).ToList())).ToList();
    }
}
