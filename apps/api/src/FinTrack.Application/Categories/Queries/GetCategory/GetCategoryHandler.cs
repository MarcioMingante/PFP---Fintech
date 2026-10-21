using FinTrack.Application.Categories.DTOs;

namespace FinTrack.Application.Categories.Queries.GetCategory;

public sealed class GetCategoryHandler(ICategoryRepository categoryRepository)
{
  public async Task<CategoryDto?> HandleAsync(
    GetCategoryQuery query,
    CancellationToken cancellationToken =default
  )
  {
    var category = await categoryRepository.GetByIdAsync(query.Id, cancellationToken);

    if (category is null)
    {
      return null;
    }

    return new CategoryDto(category.Id, category.Name);
  }
}