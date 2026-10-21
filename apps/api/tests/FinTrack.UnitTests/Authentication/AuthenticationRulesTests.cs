using FinTrack.Application.Authentication;
using FinTrack.Domain.Accounts;
using FinTrack.Domain.Categories;
using FinTrack.Domain.Users;
using FinTrack.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;

namespace FinTrack.UnitTests.Authentication;

public sealed class AuthenticationRulesTests
{
    [Theory]
    [InlineData("invalid", "Password!123456", "email")]
    [InlineData("user@example.com", "short", "password")]
    [InlineData("", "", "email")]
    public void Registration_RejectsInvalidCredentials(string email, string password, string field) =>
        Assert.Contains(field, CredentialsValidator.Validate(new(email, password)).Keys);

    [Fact]
    public void Credentials_AcceptValidValuesAndNormalizeUserEmail()
    {
        Assert.Empty(CredentialsValidator.Validate(new("USER@example.com", "Password!123456")));
        Assert.Equal("user@example.com", new User(" USER@example.com ").Email);
    }

    [Fact]
    public void Ownership_RejectsEmptyIdAndReassignment()
    {
        var owner = Guid.NewGuid();
        var account = new Account("Conta");
        var category = new Category("Categoria");
        Assert.Throws<ArgumentException>(() => account.AssignOwner(Guid.Empty));
        Assert.Throws<ArgumentException>(() => category.AssignOwner(Guid.Empty));
        account.AssignOwner(owner); category.AssignOwner(owner);
        Assert.Throws<InvalidOperationException>(() => account.AssignOwner(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => category.AssignOwner(Guid.NewGuid()));
        Assert.Equal(owner, account.UserId); Assert.Equal(owner, category.UserId);
    }

    [Fact]
    public void JwtSettings_RejectMissingOrWeakSigningKey()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "weak" }).Build();
        Assert.Throws<InvalidOperationException>(() => JwtSettings.Load(configuration));
    }
}
