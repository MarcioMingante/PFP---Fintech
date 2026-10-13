using FinTrack.Application.CreditCards.DTOs;

namespace FinTrack.Application.CreditCards.Queries.ListCreditCards;

public sealed class ListCreditCardsHandler(ICreditCardRepository creditCardRepository)
{
  public async Task<IReadOnlyList<CreditCardDto>> HandleAsync(
    ListCreditCardsQuery query,
    CancellationToken cancellationToken = default
  )
  {
    var creditCards = await creditCardRepository.ListAsync(cancellationToken);

    return creditCards.Select(creditCard => new CreditCardDto(
      creditCard.Id,
      creditCard.Name, creditCard.Limit, creditCard.ClosingDay, creditCard.DueDay
    ))
    .ToList();
  }
}