using FinTrack.Domain.Categories;

namespace FinTrack.UnitTests.Domain.Categories;

public sealed class CategoryTests
{
  [Fact]
  public void Constructor_WithValidName_CreatesCategory()
  {
    // Arrange
    const string name = "Alimentação";

    // Act
    var category = new Category(name);

    // Assert
    Assert.NotEqual(Guid.Empty, category.Id);
    Assert.Equal(name, category.Name);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("   ")]
  public void Constructor_WithEmptyName_ThrowsArgumentException(
    string name
  )
  {
    // Act
    var action = () => new Category(name);

    // Assert
    var exception = Assert.Throws<ArgumentException>(action);

    Assert.Equal("name", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithSurroundingSpaces_TrimsName()
  {
    // Arrange
    const string name = "  Alimentação  ";

    // Act
    var category = new Category(name);

    // Assert
    Assert.Equal("Alimentação", category.Name);
  }

  [Fact]
  public void Rename_WithValidName_UpdatesAndTrimsName()
  {
    // Arrange
    var category = new Category("Categoria antiga");

    // Act
    category.Rename("  Categoria nova  ");

    // Assert
    Assert.Equal("Categoria nova", category.Name);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("   ")]
  public void Rename_WithEmptyName_ThrowsAndPreservesCurrentName(
    string name
  )
  {
    // Arrange
    var category = new Category("Categoria original");

    // Act
    var action = () => category.Rename(name);

    // Assert
    var exception = Assert.Throws<ArgumentException>(action);

    Assert.Equal("name", exception.ParamName);
    Assert.Equal("Categoria original", category.Name);
  }
}