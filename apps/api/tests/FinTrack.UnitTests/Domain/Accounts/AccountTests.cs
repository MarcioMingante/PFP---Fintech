using FinTrack.Domain.Accounts;

namespace FinTrack.UnitTests.Domain.Accounts;

public sealed class AccountTests
{
  [Fact]
  public void Constructor_WithValidName_CreatesAccount()
  {
    // Arrange
    const string name = "Conta corrente";

    // Act
    var account = new Account(name);

    // Assert
    Assert.NotEqual(Guid.Empty, account.Id);
    Assert.Equal(name, account.Name);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("   ")]
  public void Constructor_WithEmptyName_ThrowsArgumentException(string name)
  {
    // Act
    var action = () => new Account(name);

    //Assert
    var exception = Assert.Throws<ArgumentException>(action);

    Assert.Equal("name", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithSurroundingSpaces_TrimsName()
  {
      // Arrange
      const string name = "  Conta corrente  ";

      // Act
      var account = new Account(name);

      // Assert
      Assert.Equal("Conta corrente", account.Name);
  }
}
