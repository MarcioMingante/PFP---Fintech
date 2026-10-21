using FinTrack.Application.Accounts;
using FinTrack.Application.Accounts.Commands.DeleteAccount;
using FinTrack.Domain.Accounts;

namespace FinTrack.UnitTests.Application.Accounts;

public sealed class DeleteAccountHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingAccount_DeletesAndReturnsTrue()
    {
        // Arrange
        var account = new Account("Conta corrente");
        var repository = new FakeAccountRepository(account);
        var handler = new DeleteAccountHandler(repository);
        var command = new DeleteAccountCommand(account.Id);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result);
        Assert.Same(account, repository.DeletedAccount);
    }

    private sealed class FakeAccountRepository(
        Account? storedAccount
    ) : IAccountRepository
    {
        public Account? DeletedAccount { get; private set; }

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
            DeletedAccount = account;

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Handle_WithUnknownId_ReturnsFalseWithoutDeleting()
    {
        // Arrange
        var repository = new FakeAccountRepository(null);
        var handler = new DeleteAccountHandler(repository);

        var command = new DeleteAccountCommand(
            Guid.NewGuid()
        );

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result);
        Assert.Null(repository.DeletedAccount);
    }
}