namespace FinTrack.Application.Accounts.Commands.UpdateAccount;

public sealed record UpdateAccountCommand(
  Guid Id,
  string Name
);
