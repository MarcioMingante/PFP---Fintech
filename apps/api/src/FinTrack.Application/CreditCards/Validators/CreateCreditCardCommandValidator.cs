using FinTrack.Application.CreditCards.Commands.CreateCreditCard;
using FinTrack.Domain.CreditCards;

namespace FinTrack.Application.CreditCards.Validators;

public sealed class CreateCreditCardCommandValidator
{
    public Dictionary<string, string[]> Validate(CreateCreditCardCommand command)
        => CreditCardRules.Validate(command.Name, command.Limit, command.ClosingDay, command.DueDay);
}
