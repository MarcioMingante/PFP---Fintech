using FinTrack.Application.Accounts;
using FinTrack.Application.Accounts.Commands.UpdateAccount;
using FinTrack.Domain.Accounts;

namespace FinTrack.UnitTests.Application.Accounts;

public sealed class UpdateAccountHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingAccount_UpdatesAndReturnsAccount()
    {
        // Arrange
        var account = new Account("Conta antiga");
        var repository = new FakeAccountRepository(account);
        var handler = new UpdateAccountHandler(repository);

        var command = new UpdateAccountCommand(
            account.Id,
            "Conta nova"
        );

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(repository.UpdatedAccount);
        Assert.Equal("Conta nova", repository.UpdatedAccount.Name);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal("Conta nova", result.Name);
    }

    private sealed class FakeAccountRepository(
        Account? storedAccount
    ) : IAccountRepository
    {
        public Account? UpdatedAccount { get; private set; }

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
            UpdatedAccount = account;

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Handle_WithUnknownId_ReturnsNullWithoutUpdating()
    {
        // Arrange
        var repository = new FakeAccountRepository(null);
        var handler = new UpdateAccountHandler(repository);

        var command = new UpdateAccountCommand(
            Guid.NewGuid(),
            "Conta nova"
        );

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Null(result);
        Assert.Null(repository.UpdatedAccount);
    }
}