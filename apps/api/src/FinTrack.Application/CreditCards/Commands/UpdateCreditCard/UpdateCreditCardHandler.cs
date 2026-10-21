using FinTrack.Application.CreditCards.DTOs;
using FinTrack.Domain.CreditCards;

namespace FinTrack.Application.CreditCards.Commands.UpdateCreditCard;

public sealed class UpdateCreditCardHandler(
  ICreditCardRepository creditCardRepository
)
{
  public async Task<CreditCardDto?> HandleAsync(
    UpdateCreditCardCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var creditCard = await creditCardRepository.GetByIdAsync(
      command.Id,
      cancellationToken
    );

    if (creditCard is null)
    {
      return null;
    }

    creditCard.Update(command.Name, command.Limit, command.ClosingDay, command.DueDay);

    await creditCardRepository.UpdateAsync(
      creditCard,
      cancellationToken
    );

    return new CreditCardDto(
      creditCard.Id,
      creditCard.Name, creditCard.Limit, creditCard.ClosingDay, creditCard.DueDay
    );
  }
}