using FinTrack.Application.Categories.DTOs;

namespace FinTrack.Application.Categories.Queries.ListCategories;

public sealed class ListCategoriesHandler(
  ICategoryRepository categoryRepository
)
{
  public async Task<IReadOnlyList<CategoryDto>> HandleAsync(
    ListCategoriesQuery query,
    CancellationToken cancellationToken = default
  )
  {
    var categories = await categoryRepository.ListAsync(cancellationToken);

    return categories.Select(category => new CategoryDto(
      category.Id,
      category.Name
    )).ToList();
  }
}