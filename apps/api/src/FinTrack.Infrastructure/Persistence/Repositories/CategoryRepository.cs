using FinTrack.Application.Categories;
using FinTrack.Domain.Categories;
using FinTrack.Infrastructure.Persistence;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(FinTrackDbContext dbContext) : ICategoryRepository
{
  public async Task AddAsync(
    Category category,
    CancellationToken cancellationToken = default
  )
  {
    dbContext.Categories.Add(category);

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}