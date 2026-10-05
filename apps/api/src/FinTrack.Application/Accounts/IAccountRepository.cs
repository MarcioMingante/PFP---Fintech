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

  Task<Account?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default
  );

  Task UpdateAsync(
    Account account,
    CancellationToken cancellationToken = default
  );
}
