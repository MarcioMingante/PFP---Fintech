using FinTrack.Application.Purchases;
using FinTrack.Domain.Purchases;
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
        database.Purchases.Add(purchase);
        try { await database.SaveChangesAsync(cancellationToken); return purchase; }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_purchases_CreditCardId_IdempotencyKey" })
        {
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
