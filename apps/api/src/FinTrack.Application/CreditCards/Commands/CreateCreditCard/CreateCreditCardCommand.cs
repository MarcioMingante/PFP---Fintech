namespace FinTrack.Application.CreditCards.Commands.CreateCreditCard;

public sealed record CreateCreditCardCommand(string Name, decimal Limit, int ClosingDay, int DueDay);
