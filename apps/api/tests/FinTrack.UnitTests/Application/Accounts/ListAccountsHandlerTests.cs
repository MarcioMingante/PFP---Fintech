using FinTrack.Application.Accounts;
using FinTrack.Application.Accounts.Queries.ListAccounts;
using FinTrack.Domain.Accounts;

namespace FinTrack.UnitTests.Application.Accounts;

public sealed class ListAccountsHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsMappedAccounts()
  {
    // Arrange
    var firstAccount = new Account("Carteira");
    var secondAccount = new Account("Conta corrente");

    var repository = new FakeAccountRepository(
      [firstAccount, secondAccount]
    );

    var handler = new ListAccountsHandler(repository);
    var query = new ListAccountsQuery();

    // Act
    var result = await handler.HandleAsync(query);

    // Assert
    Assert.Collection(
      result,
      first =>
      {
        Assert.Equal(firstAccount.Id, first.Id);
        Assert.Equal(firstAccount.Name, first.Name);
      },
        second =>
      {
        Assert.Equal(secondAccount.Id, second.Id);
        Assert.Equal(secondAccount.Name, second.Name);
      }
    );
  }

  private sealed class FakeAccountRepository(
    IReadOnlyList<Account> accounts
  ) : IAccountRepository
  {
    public Task AddAsync(
      Account account,
      CancellationToken cancellationToken = default
    )
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<Account>> ListAsync(
      CancellationToken cancellationToken = default
    )
    {
      return Task.FromResult(accounts);
    }

    public Task<Account?> GetByIdAsync(
      Guid id,
      CancellationToken cancellationToken = default
    )
    {
      var account = accounts.FirstOrDefault(
        account => account.Id == id
      );

      return Task.FromResult(account);
    }

    public Task UpdateAsync(
        Account account,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public Task DeleteAsync(
        Account account,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }
  }
}