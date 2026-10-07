using FinTrack.Application.Categories;
using FinTrack.Application.Categories.Commands.CreateCategory;
using FinTrack.Domain.Categories;

namespace FinTrack.UnitTests.Application.Categories;

public sealed class CreateCategoryHandlerTests
{
  [Fact]
  public async Task Handle_WithValidCommand_CreatesAndPersistsCategory()
  {
    // Arrange
    var repository = new FakeCategoryRepository();
    var handler = new CreateCategoryHandler(repository);
    var command = new CreateCategoryCommand("Alimentação");

    // Act
    var result = await handler.HandleAsync(command);

    // Assert
    Assert.NotNull(repository.AddedCategory);
    Assert.Equal("Alimentação", repository.AddedCategory.Name);
    Assert.Equal(repository.AddedCategory.Id, result.Id);
    Assert.Equal(repository.AddedCategory.Name, result.Name);
  }

  private sealed class FakeCategoryRepository : ICategoryRepository
  {
    public Category? AddedCategory { get; private set; }

    public Task AddAsync(
      Category category,
      CancellationToken cancellationToken = default
    )
    {
      AddedCategory = category;

      return Task.CompletedTask;
    }
  }
}