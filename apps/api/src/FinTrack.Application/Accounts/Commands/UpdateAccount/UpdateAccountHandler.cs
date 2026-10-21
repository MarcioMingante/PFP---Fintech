using FinTrack.Application.Accounts.DTOs;
using FinTrack.Domain.Accounts;

namespace FinTrack.Application.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountHandler(
  IAccountRepository accountRepository
)
{
  public async Task<AccountDto?> HandleAsync(
    UpdateAccountCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var account = await accountRepository.GetByIdAsync(
      command.Id,
      cancellationToken
    );

    if (account is null)
    {
      return null;
    }

    account.Rename(command.Name);

    await accountRepository.UpdateAsync(
      account,
      cancellationToken
    );

    return new AccountDto(
      account.Id,
      account.Name
    );
  }
}