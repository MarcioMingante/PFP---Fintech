using FinTrack.Application.Accounts.DTOs;
using FinTrack.Domain.Accounts;

namespace FinTrack.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountHandler(IAccountRepository accountRepository)
{
  public async Task<AccountDto> HandleAsync(
    CreateAccountCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var account = new Account(command.Name);

    await accountRepository.AddAsync(account, cancellationToken);

    return new AccountDto(account.Id, account.Name);
  }
}