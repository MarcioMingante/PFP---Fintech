namespace FinTrack.Application.CreditCards.DTOs;

public sealed record CreditCardDto(Guid Id, string Name, decimal Limit, int ClosingDay, int DueDay);
