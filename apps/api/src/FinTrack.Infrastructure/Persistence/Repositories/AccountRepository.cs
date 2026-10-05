using FinTrack.Application.Accounts;
using FinTrack.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

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

  public async Task<IReadOnlyList<Account>> ListAsync(
    CancellationToken cancellationToken = default
  )
  {
    return await dbContext.Accounts
      .AsNoTracking()
      .OrderBy(account => account.Name)
      .ToListAsync(cancellationToken);
  }

  public async Task<Account?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default
  )
  {
    return await dbContext.Accounts
      .AsNoTracking()
      .FirstOrDefaultAsync(
        account => account.Id == id,
        cancellationToken
      );
  }

  public async Task UpdateAsync(
    Account account,
    CancellationToken cancellationToken = default
  )
  {
    dbContext.Accounts.Update(account);

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(
    Account account,
    CancellationToken cancellationToken = default
  )
  {
    dbContext.Accounts.Remove(account);

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
