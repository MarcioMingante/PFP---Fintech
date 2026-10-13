using FinTrack.Domain.Purchases;

namespace FinTrack.Application.Purchases;

public sealed record InstallmentDto(Guid Id, int Number, decimal Amount, DateOnly ClosingDate, DateOnly DueDate);
public sealed record PurchaseDto(Guid Id, Guid CreditCardId, string Description, decimal Amount,
    int InstallmentCount, DateOnly PurchaseDate, IReadOnlyList<InstallmentDto> Installments)
{
    public static PurchaseDto From(Purchase purchase) => new(purchase.Id, purchase.CreditCardId,
        purchase.Description, purchase.Amount, purchase.InstallmentCount, purchase.PurchaseDate,
        purchase.Installments.OrderBy(item => item.Number).Select(item => new InstallmentDto(
            item.Id, item.Number, item.Amount, item.ClosingDate, item.DueDate)).ToList());
}
