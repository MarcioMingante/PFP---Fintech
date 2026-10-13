using FinTrack.Application.Categories;
using FinTrack.Domain.Categories;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(FinTrackDbContext dbContext) : ICategoryRepository
{
  public async Task AddAsync(
    Category category,
    CancellationToken cancellationToken = default
  )
  {
    category.AssignOwner(dbContext.CurrentUserId ?? throw new UnauthorizedAccessException());
    dbContext.Categories.Add(category);

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<Category>> ListAsync(
    CancellationToken cancellationToken = default
  )
  {
    return await dbContext.Categories
      .AsNoTracking()
      .OrderBy(category => category.Name)
      .ToListAsync(cancellationToken);
  }

  public async Task<Category?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default
  )
  {
    return await dbContext.Categories
      .AsNoTracking()
      .FirstOrDefaultAsync(
        category => category.Id == id,
        cancellationToken
      );
  }

  public async Task UpdateAsync(
    Category category,
    CancellationToken cancellationToken = default
  )
  {
    dbContext.Categories.Update(category);

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
