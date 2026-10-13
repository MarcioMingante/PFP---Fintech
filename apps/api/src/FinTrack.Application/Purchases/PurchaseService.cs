using FinTrack.Application.CreditCards;
using FinTrack.Domain.Purchases;

namespace FinTrack.Application.Purchases;

public sealed record CreatePurchaseCommand(string Description, decimal Amount, int InstallmentCount,
    DateOnly PurchaseDate, Guid IdempotencyKey);
public enum PurchaseStatus { Created, Replayed, NotFound, Conflict }
public sealed record PurchaseResult(PurchaseStatus Status, PurchaseDto? Purchase = null);

public sealed class PurchaseService(ICreditCardRepository cards, IPurchaseRepository purchases)
{
    public async Task<PurchaseResult> CreateAsync(Guid cardId, CreatePurchaseCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return new(PurchaseStatus.NotFound);
        var existing = await purchases.FindAsync(cardId, command.IdempotencyKey, cancellationToken);
        if (existing is not null) return Replay(existing, command);
        var purchase = new Purchase(card, command.Description, command.Amount, command.InstallmentCount,
            command.PurchaseDate, command.IdempotencyKey);
        var saved = await purchases.AddOrGetAsync(purchase, cancellationToken);
        return saved.Id == purchase.Id ? new(PurchaseStatus.Created, PurchaseDto.From(saved)) : Replay(saved, command);
    }

    public async Task<IReadOnlyList<PurchaseDto>?> ListAsync(Guid cardId, CancellationToken cancellationToken)
    {
        if (await cards.GetByIdAsync(cardId, cancellationToken) is null) return null;
        return (await purchases.ListAsync(cardId, cancellationToken)).Select(PurchaseDto.From).ToList();
    }

    private static PurchaseResult Replay(Purchase existing, CreatePurchaseCommand command)
        => existing.Description == command.Description.Trim() && existing.Amount == command.Amount
            && existing.InstallmentCount == command.InstallmentCount && existing.PurchaseDate == command.PurchaseDate
            ? new(PurchaseStatus.Replayed, PurchaseDto.From(existing)) : new(PurchaseStatus.Conflict);
}
