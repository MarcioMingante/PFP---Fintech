namespace FinTrack.Application.CreditCards.Commands.DeleteCreditCard;

public sealed class DeleteCreditCardHandler(ICreditCardRepository creditCardRepository)
{
  public async Task<bool> HandleAsync(
    DeleteCreditCardCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var creditCard = await creditCardRepository.GetByIdAsync(
      command.Id,
      cancellationToken
    );

    if (creditCard is null)
    {
      return false;
    }

    await creditCardRepository.DeleteAsync(
      creditCard,
      cancellationToken
    );

    return true;
  }
}