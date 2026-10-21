using FinTrack.Application.Categories.Commands.CreateCategory;
using FinTrack.Application.Categories.Validators;

namespace FinTrack.UnitTests.Application.Categories;

public sealed class CreateCategoryCommandValidatorTests
{
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("   ")]
  public void Validate_WithEmptyName_ReturnsNameError(string name)
  {
    // Arrange
    var validator = new CreateCategoryCommandValidator();
    var command = new CreateCategoryCommand(name);

    // Act
    var errors = validator.Validate(command);

    // Assert
    Assert.True(errors.ContainsKey("name"));
    Assert.Equal(
      "Nome da categoria não pode ser vazio.",
      errors["name"][0]
    );
  }

  [Fact]
  public void Validate_WithValidName_ReturnsNoErrors()
  {
    // Arrange
    var validator = new CreateCategoryCommandValidator();
    var command = new CreateCategoryCommand("Alimentação");

    // Act
    var errors = validator.Validate(command);

    // Assert
    Assert.Empty(errors);
  }
}
