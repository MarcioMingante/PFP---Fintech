namespace FinTrack.Api.Contracts.CreditCards;

public sealed record UpdateCreditCardRequest(string Name, decimal Limit, int ClosingDay, int DueDay);
