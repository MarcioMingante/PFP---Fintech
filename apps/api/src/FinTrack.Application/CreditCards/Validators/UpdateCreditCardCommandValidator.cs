using FinTrack.Application.CreditCards.Commands.UpdateCreditCard;
using FinTrack.Domain.CreditCards;

namespace FinTrack.Application.CreditCards.Validators;

public sealed class UpdateCreditCardCommandValidator
{
    public Dictionary<string, string[]> Validate(UpdateCreditCardCommand command)
        => CreditCardRules.Validate(command.Name, command.Limit, command.ClosingDay, command.DueDay);
}
