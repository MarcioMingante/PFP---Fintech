using FinTrack.Application.Accounts;
using FinTrack.Application.Accounts.Queries.GetAccount;
using FinTrack.Domain.Accounts;

namespace FinTrack.UnitTests.Application.Accounts;

public sealed class GetAccountHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingId_ReturnsMappedAccount()
    {
        // Arrange
        var account = new Account("Conta corrente");
        var repository = new FakeAccountRepository(account);
        var handler = new GetAccountHandler(repository);
        var query = new GetAccountQuery(account.Id);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal(account.Name, result.Name);
    }

    private sealed class FakeAccountRepository(
        Account? storedAccount
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
            throw new NotSupportedException();
        }

        public Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            var result = storedAccount?.Id == id
                ? storedAccount
                : null;

            return Task.FromResult(result);
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

    [Fact]
    public async Task Handle_WithUnknownId_ReturnsNull()
    {
        // Arrange
        var repository = new FakeAccountRepository(null);
        var handler = new GetAccountHandler(repository);
        var query = new GetAccountQuery(Guid.NewGuid());

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.Null(result);
    }
}
