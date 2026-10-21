using FinTrack.Domain.Purchases;

namespace FinTrack.Application.Purchases;

public interface IPurchaseRepository
{
    Task<Purchase?> FindAsync(Guid cardId, Guid key, CancellationToken cancellationToken);
    Task<Purchase> AddOrGetAsync(Purchase purchase, CancellationToken cancellationToken);
    Task<IReadOnlyList<Purchase>> ListAsync(Guid cardId, CancellationToken cancellationToken);
}
