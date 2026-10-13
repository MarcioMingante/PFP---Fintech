namespace FinTrack.Api.Contracts.CreditCards;

public sealed record CreateCreditCardRequest(string Name, decimal Limit, int ClosingDay, int DueDay);
