using FinTrack.Application.CreditCards;
using FinTrack.Domain.CreditCards;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class CreditCardRepository(FinTrackDbContext dbContext) : ICreditCardRepository
{
  public async Task AddAsync(
    CreditCard creditCard,
    CancellationToken cancellationToken = default
  )
  {
    creditCard.AssignOwner(dbContext.CurrentUserId ?? throw new UnauthorizedAccessException());
    dbContext.CreditCards.Add(creditCard);

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<CreditCard>> ListAsync(
    CancellationToken cancellationToken = default
  )
  {
    return await dbContext.CreditCards
      .AsNoTracking()
      .OrderBy(creditCard => creditCard.Name)
      .ToListAsync(cancellationToken);
  }

  public async Task<CreditCard?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default
  )
  {
    return await dbContext.CreditCards
      .AsNoTracking()
      .FirstOrDefaultAsync(
        creditCard => creditCard.Id == id,
        cancellationToken
      );
  }

  public async Task UpdateAsync(
    CreditCard creditCard,
    CancellationToken cancellationToken = default
  )
  {
    dbContext.CreditCards.Update(creditCard);

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(
    CreditCard creditCard,
    CancellationToken cancellationToken = default
  )
  {
    dbContext.CreditCards.Remove(creditCard);

    try { await dbContext.SaveChangesAsync(cancellationToken); }
    catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException
        { SqlState: Npgsql.PostgresErrorCodes.ForeignKeyViolation })
    {
      throw new InvalidOperationException("Cartão com compras não pode ser excluído.", exception);
    }

  }
}
