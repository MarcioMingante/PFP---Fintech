using FinTrack.Application.Categories;
using FinTrack.Application.Categories.Queries.ListCategories;
using FinTrack.Domain.Categories;

namespace FinTrack.UnitTests.Application.Categories;

public sealed class ListCategoriesHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsMappedCategories()
  {
    // Arrange
    var firstCategory = new Category("Alimentação");
    var secondCategory = new Category("Transporte");
    var repository = new FakeCategoryRepository(
      [firstCategory, secondCategory]
    );
    var handler = new ListCategoriesHandler(repository);
    var query = new ListCategoriesQuery();

    // Act
    var result = await handler.HandleAsync(query);

    // Assert
    Assert.Collection(
      result,
      first =>
      {
        Assert.Equal(firstCategory.Id, first.Id);
        Assert.Equal(firstCategory.Name, first.Name);
      },
      second =>
      {
        Assert.Equal(secondCategory.Id, second.Id);
        Assert.Equal(secondCategory.Name, second.Name);
      }
    );
  }

  [Fact]
  public async Task Handle_WithNoCategories_ReturnsEmptyList()
  {
    // Arrange
    var repository = new FakeCategoryRepository([]);
    var handler = new ListCategoriesHandler(repository);

    // Act
    var result = await handler.HandleAsync(new ListCategoriesQuery());

    // Assert
    Assert.Empty(result);
  }

  private sealed class FakeCategoryRepository(
    IReadOnlyList<Category> categories
  ) : ICategoryRepository
  {
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
      return Task.FromResult(categories);
    }
  }
}
