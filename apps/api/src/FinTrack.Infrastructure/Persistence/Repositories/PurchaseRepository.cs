using FinTrack.Application.Purchases;
using FinTrack.Domain.Purchases;
using FinTrack.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class PurchaseRepository(FinTrackDbContext database) : IPurchaseRepository
{
    public Task<Purchase?> FindAsync(Guid cardId, Guid key, CancellationToken cancellationToken)
        => database.Purchases.AsNoTracking().Include(item => item.Installments)
            .FirstOrDefaultAsync(item => item.CreditCardId == cardId && item.IdempotencyKey == key, cancellationToken);

    public async Task<Purchase> AddOrGetAsync(Purchase purchase, CancellationToken cancellationToken)
    {
        if (purchase.UserId != database.CurrentUserId) throw new UnauthorizedAccessException();
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var installment in purchase.Installments.OrderBy(item => item.ClosingDate).ThenBy(item => item.DueDate))
            {
                var candidate = new CreditCardInvoice(purchase.CreditCardId, purchase.UserId, installment.ClosingDate, installment.DueDate);
                // The unique cycle index serializes concurrent purchases for the same invoice.
                await database.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO credit_card_invoices (id, credit_card_id, user_id, closing_date, due_date)
                    VALUES ({candidate.Id}, {candidate.CreditCardId}, {candidate.UserId}, {candidate.ClosingDate}, {candidate.DueDate})
                    ON CONFLICT (credit_card_id, closing_date, due_date) DO NOTHING
                    """, cancellationToken);
                var invoice = await database.CreditCardInvoices.AsNoTracking().SingleAsync(item =>
                    item.CreditCardId == purchase.CreditCardId && item.ClosingDate == installment.ClosingDate
                    && item.DueDate == installment.DueDate, cancellationToken);
                installment.AssignInvoice(invoice, purchase.CreditCardId, purchase.UserId);
            }
            database.Purchases.Add(purchase);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return purchase;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_purchases_CreditCardId_IdempotencyKey" })
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            database.ChangeTracker.Clear();
            return await FindAsync(purchase.CreditCardId, purchase.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("Compra duplicada não encontrada.");
        }
    }

    public async Task<IReadOnlyList<Purchase>> ListAsync(Guid cardId, CancellationToken cancellationToken)
        => await database.Purchases.AsNoTracking().Include(item => item.Installments)
            .Where(item => item.CreditCardId == cardId).OrderByDescending(item => item.PurchaseDate)
            .ThenBy(item => item.Id).ToListAsync(cancellationToken);
}
