using FinTrack.Application.Categories.Commands.UpdateCategory;
using FinTrack.Application.Categories.Validators;

namespace FinTrack.UnitTests.Application.Categories;

public sealed class UpdateCategoryCommandValidatorTests
{
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("   ")]
  public void Validate_WithEmptyName_ReturnsNameError(string name)
  {
    var validator = new UpdateCategoryCommandValidator();

    var errors = validator.Validate(
      new UpdateCategoryCommand(Guid.NewGuid(), name)
    );

    Assert.True(errors.ContainsKey("name"));
    Assert.Equal("Nome da categoria não pode ser vazio.", errors["name"][0]);
  }

  [Fact]
  public void Validate_WithValidName_ReturnsNoErrors()
  {
    var validator = new UpdateCategoryCommandValidator();

    var errors = validator.Validate(
      new UpdateCategoryCommand(Guid.NewGuid(), "Alimentação")
    );

    Assert.Empty(errors);
  }
}
