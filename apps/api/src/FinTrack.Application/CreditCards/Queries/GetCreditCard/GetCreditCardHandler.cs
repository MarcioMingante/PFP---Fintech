using FinTrack.Application.CreditCards.DTOs;
using FinTrack.Domain.CreditCards;

namespace FinTrack.Application.CreditCards.Queries.GetCreditCard;

public sealed class GetCreditCardHandler(
  ICreditCardRepository creditCardRepository
)
{
  public async Task<CreditCardDto?> HandleAsync(
    GetCreditCardQuery query,
    CancellationToken cancellationToken = default
  )
  {
    var creditCard = await creditCardRepository.GetByIdAsync(
      query.Id,
      cancellationToken
    );

    if (creditCard is null)
    {
      return null;
    }

    return new CreditCardDto(
      creditCard.Id,
      creditCard.Name, creditCard.Limit, creditCard.ClosingDay, creditCard.DueDay
    );
  }
}