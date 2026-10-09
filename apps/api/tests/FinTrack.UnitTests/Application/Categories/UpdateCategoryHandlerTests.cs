using FinTrack.Application.Categories;
using FinTrack.Application.Categories.Commands.UpdateCategory;
using FinTrack.Domain.Categories;

namespace FinTrack.UnitTests.Application.Categories;

public sealed class UpdateCategoryHandlerTests
{
  [Fact]
  public async Task Handle_WithExistingCategory_UpdatesAndReturnsCategory()
  {
    var category = new Category("Nome antigo");
    var repository = new FakeCategoryRepository(category);
    var handler = new UpdateCategoryHandler(repository);

    var result = await handler.HandleAsync(
      new UpdateCategoryCommand(category.Id, "  Nome novo  ")
    );

    Assert.NotNull(result);
    Assert.Same(category, repository.UpdatedCategory);
    Assert.Equal(category.Id, result.Id);
    Assert.Equal("Nome novo", result.Name);
    Assert.Equal("Nome novo", category.Name);
  }

  [Fact]
  public async Task Handle_WithUnknownId_ReturnsNullWithoutUpdating()
  {
    var category = new Category("Nome original");
    var repository = new FakeCategoryRepository(category);
    var handler = new UpdateCategoryHandler(repository);

    var result = await handler.HandleAsync(
      new UpdateCategoryCommand(Guid.NewGuid(), "Nome novo")
    );

    Assert.Null(result);
    Assert.Null(repository.UpdatedCategory);
    Assert.Equal("Nome original", category.Name);
  }

  private sealed class FakeCategoryRepository(Category category)
    : ICategoryRepository
  {
    public Category? UpdatedCategory { get; private set; }

    public Task<Category?> GetByIdAsync(
      Guid id,
      CancellationToken cancellationToken = default
    ) => Task.FromResult<Category?>(category.Id == id ? category : null);

    public Task UpdateAsync(
      Category category,
      CancellationToken cancellationToken = default
    )
    {
      UpdatedCategory = category;
      return Task.CompletedTask;
    }

    public Task AddAsync(
      Category category,
      CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    public Task<IReadOnlyList<Category>> ListAsync(
      CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();
  }
}
