using FinTrack.Application.Accounts;
using FinTrack.Domain.Accounts;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(FinTrackDbContext dbContext) : IAccountRepository
{
  public async Task AddAsync(
    Account account,
    CancellationToken cancellationToken = default
  )
  {
    dbContext.Accounts.Add(account);

    await dbContext.SaveChangesAsync(cancellationToken); 
  }
}
