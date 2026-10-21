using FinTrack.Application.Categories;
using FinTrack.Application.Categories.Queries.GetCategory;
using FinTrack.Domain.Categories;

namespace FinTrack.UnitTests.Application.Categories;

public sealed class GetCategoryHandlerTests
{
  [Fact]
  public async Task Handle_WithExistingId_ReturnsMappedCategory()
  {
    // Arrange
    var category = new Category("Alimentação");
    var repository = new FakeCategoryRepository(category);
    var handler = new GetCategoryHandler(repository);

    // Act
    var result = await handler.HandleAsync(new GetCategoryQuery(category.Id));

    // Assert
    Assert.NotNull(result);
    Assert.Equal(category.Id, result.Id);
    Assert.Equal(category.Name, result.Name);
  }

  [Fact]
  public async Task Handle_WithUnknownId_ReturnsNull()
  {
    // Arrange
    var category = new Category("Alimentação");
    var repository = new FakeCategoryRepository(category);
    var handler = new GetCategoryHandler(repository);

    // Act
    var result = await handler.HandleAsync(new GetCategoryQuery(Guid.NewGuid()));

    // Assert
    Assert.Null(result);
  }

  private sealed class FakeCategoryRepository(Category category)
    : ICategoryRepository
  {
    public Task UpdateAsync(
      Category category,
      CancellationToken cancellationToken = default
    )
    {
      throw new NotSupportedException();
    }

    public Task<Category?> GetByIdAsync(
      Guid id,
      CancellationToken cancellationToken = default
    )
    {
      return Task.FromResult<Category?>(category.Id == id ? category : null);
    }

    public Task AddAsync(
      Category category,
      CancellationToken cancellationToken = default
    )
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<Category>> ListAsync(
      CancellationToken cancellationToken = default
    )
    {
      throw new NotSupportedException();
    }
  }
}
