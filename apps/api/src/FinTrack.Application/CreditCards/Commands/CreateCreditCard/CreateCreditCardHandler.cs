using FinTrack.Application.CreditCards.DTOs;
using FinTrack.Domain.CreditCards;

namespace FinTrack.Application.CreditCards.Commands.CreateCreditCard;

public sealed class CreateCreditCardHandler(ICreditCardRepository creditCardRepository)
{
  public async Task<CreditCardDto> HandleAsync(
    CreateCreditCardCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var creditCard = new CreditCard(command.Name, command.Limit, command.ClosingDay, command.DueDay);

    await creditCardRepository.AddAsync(creditCard, cancellationToken);

    return new CreditCardDto(creditCard.Id, creditCard.Name, creditCard.Limit, creditCard.ClosingDay, creditCard.DueDay);
  }
}