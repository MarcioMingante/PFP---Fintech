using FinTrack.Application.Accounts;
using FinTrack.Application.Accounts.Commands.CreateAccount;
using FinTrack.Domain.Accounts;

namespace FinTrack.UnitTests.Application.Accounts;

public sealed class CreateAccountHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesAndPersistsAccount()
    {
        // Arrange
        var repository = new FakeAccountRepository();
        var handler = new CreateAccountHandler(repository);
        var command = new CreateAccountCommand("Conta corrente");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(repository.AddedAccount);
        Assert.Equal("Conta corrente", repository.AddedAccount.Name);
        Assert.Equal(repository.AddedAccount.Id, result.Id);
        Assert.Equal(repository.AddedAccount.Name, result.Name);
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        public Account? AddedAccount { get; private set; }

        public Task AddAsync(
            Account account,
            CancellationToken cancellationToken = default
        )
        {
            AddedAccount = account;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Account>> ListAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<IReadOnlyList<Account>>([]);
        }

        public Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<Account?>(null);
        }
    }
}