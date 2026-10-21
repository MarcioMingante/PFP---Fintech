namespace FinTrack.Application.Accounts.Commands.DeleteAccount;

public sealed class DeleteAccountHandler(IAccountRepository accountRepository)
{
  public async Task<bool> HandleAsync(
    DeleteAccountCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var account = await accountRepository.GetByIdAsync(
      command.Id,
      cancellationToken
    );

    if (account is null)
    {
      return false;
    }

    await accountRepository.DeleteAsync(
      account,
      cancellationToken
    );

    return true;
  }
}