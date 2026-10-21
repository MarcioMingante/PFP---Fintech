using FinTrack.Application.Accounts.Commands.UpdateAccount;
using FinTrack.Application.Accounts.Validators;

namespace FinTrack.UnitTests.Application.Accounts;

public sealed class UpdateAccountCommandValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ReturnsNameError(string name)
    {
        // Arrange
        var validator = new UpdateAccountCommandValidator();

        var command = new UpdateAccountCommand(
            Guid.NewGuid(),
            name
        );

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
        var validator = new UpdateAccountCommandValidator();

        var command = new UpdateAccountCommand(
            Guid.NewGuid(),
            "Conta nova"
        );

        // Act
        var errors = validator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }
}