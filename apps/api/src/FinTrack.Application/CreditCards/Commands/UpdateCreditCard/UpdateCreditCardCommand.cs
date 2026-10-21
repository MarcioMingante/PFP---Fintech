namespace FinTrack.Application.CreditCards.Commands.UpdateCreditCard;

public sealed record UpdateCreditCardCommand(
  Guid Id,
  string Name, decimal Limit, int ClosingDay, int DueDay
);
