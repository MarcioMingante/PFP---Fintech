using FinTrack.Domain.Purchases;

namespace FinTrack.Application.Purchases;

public sealed class CreatePurchaseCommandValidator
{
    public Dictionary<string, string[]> Validate(CreatePurchaseCommand command)
        => Purchase.Validate(command.Description, command.Amount, command.InstallmentCount,
            command.PurchaseDate, command.IdempotencyKey);
}
