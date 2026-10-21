using FinTrack.Application.Categories.DTOs;

namespace FinTrack.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler(ICategoryRepository categoryRepository)
{
  public async Task<CategoryDto?> HandleAsync(
    UpdateCategoryCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var category = await categoryRepository.GetByIdAsync(command.Id, cancellationToken);

    if (category is null)
    {
      return null;
    }

    category.Rename(command.Name);

    await categoryRepository.UpdateAsync(category, cancellationToken);

    return new CategoryDto(category.Id, category.Name);
  }
}
