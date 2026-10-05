using FinTrack.Application.Accounts.DTOs;

namespace FinTrack.Application.Accounts.Queries.ListAccounts;

public sealed class ListAccountsHandler(IAccountRepository accountRepository)
{
  public async Task<IReadOnlyList<AccountDto>> HandleAsync(
    ListAccountsQuery query,
    CancellationToken cancellationToken = default
  )
  {
    var accounts = await accountRepository.ListAsync(cancellationToken);

    return accounts.Select(account => new AccountDto(
      account.Id,
      account.Name
    ))
    .ToList();
  }
}