using FinTrack.Domain.Categories;

namespace FinTrack.Application.Categories;

public interface ICategoryRepository
{
  Task AddAsync(
    Category category,
    CancellationToken cancellationToken = default
  );
}
