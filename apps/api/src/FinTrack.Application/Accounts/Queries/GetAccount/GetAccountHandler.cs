using FinTrack.Application.Accounts.DTOs;
using FinTrack.Domain.Accounts;

namespace FinTrack.Application.Accounts.Queries.GetAccount;

public sealed class GetAccountHandler(
  IAccountRepository accountRepository
)
{
  public async Task<AccountDto?> HandleAsync(
    GetAccountQuery query,
    CancellationToken cancellationToken = default
  )
  {
    var account = await accountRepository.GetByIdAsync(
      query.Id,
      cancellationToken
    );

    if (account is null)
    {
      return null;
    }

    return new AccountDto(
      account.Id,
      account.Name
    );
  }
}