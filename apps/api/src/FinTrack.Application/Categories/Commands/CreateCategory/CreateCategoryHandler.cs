using FinTrack.Application.Categories.DTOs;
using FinTrack.Domain.Categories;

namespace FinTrack.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryHandler(ICategoryRepository categoryRepository)
{
  public async Task<CategoryDto> HandleAsync(
    CreateCategoryCommand command,
    CancellationToken cancellationToken = default
  )
  {
    var category = new Category(command.Name);

    await categoryRepository.AddAsync(category, cancellationToken);

    return new CategoryDto(category.Id, category.Name);
  }
}