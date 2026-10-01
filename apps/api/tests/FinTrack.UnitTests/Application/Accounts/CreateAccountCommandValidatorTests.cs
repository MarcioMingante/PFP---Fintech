using FinTrack.Application.Accounts.Commands.CreateAccount;
using FinTrack.Application.Accounts.Validators;

namespace FinTrack.UnitTests.Application.Accounts;

public sealed class CreateAccountCommandValidatorTests
{
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("   ")]
  public void Validate_WithEmptyName_ReturnsNameError(string name)
  {
    // Arrange
    var validator = new CreateAccountCommandValidator();
    var command = new CreateAccountCommand(name);

    // Act
    var errors = validator.Validate(command);

    // Assert
    Assert.True(errors.ContainsKey("name"));
    Assert.Equal(
      "Nome da conta não pode ser vazio.",
      errors["name"][0]
    );
  }

  [Fact]
  public void Validate_WithValidName_ReturnsNoErrors()
  {
    // Arrange
    var validator = new CreateAccountCommandValidator();
    var command = new CreateAccountCommand("Conta corrente");

    // Act
    var errors = validator.Validate(command);

    // Assert
    Assert.Empty(errors);
  }
}