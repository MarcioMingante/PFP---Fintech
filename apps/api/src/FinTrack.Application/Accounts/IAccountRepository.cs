using FinTrack.Domain.Accounts;

namespace FinTrack.Application.Accounts;

public interface IAccountRepository
{
  Task AddAsync(
    Account account,
    CancellationToken cancellationToken = default
  );

  Task<IReadOnlyList<Account>> ListAsync(
    CancellationToken cancellationToken = default
  );
}
