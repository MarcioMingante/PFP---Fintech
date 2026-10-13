using FinTrack.Application.Accounts;
using FinTrack.Application.Categories;
using FinTrack.Application.Authentication;
using FinTrack.Application.Recurring;
using FinTrack.Domain.Transactions;
using FinTrack.Domain.Recurring;

namespace FinTrack.Application.Transactions;

public sealed record CreateTransactionCommand(Guid AccountId, Guid? CategoryId, string Description, decimal Amount, string Type, DateOnly OccurredOn);
public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
}
public sealed class TransactionService(ITransactionRepository repository, IAccountRepository accounts,
    ICategoryRepository categories, ICurrentUser user, TimeProvider clock)
{
    public Dictionary<string, string[]> Validate(CreateTransactionCommand command)
    {
        var type = ParseType(command.Type);
        var errors = RecurringTransaction.Validate(command.Description, command.Amount, type, command.OccurredOn, null);
        if (errors.Remove("startDate", out var dateErrors)) errors["occurredOn"] = dateErrors;
        if (command.OccurredOn > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)) errors["occurredOn"] = ["Lançamento não pode ter data futura."];
        if (command.AccountId == Guid.Empty) errors["accountId"] = ["Conta obrigatória."];
        if (command.CategoryId == Guid.Empty) errors["categoryId"] = ["Categoria inválida."];
        return errors;
    }
    private static TransactionType ParseType(string type) => type switch { "Income" => TransactionType.Income, "Expense" => TransactionType.Expense, _ => (TransactionType)0 };
    public async Task<TransactionDto?> CreateAsync(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        if (Validate(command).Count > 0) throw new ArgumentException("Lançamento inválido.", nameof(command));
        if (await accounts.GetByIdAsync(command.AccountId, cancellationToken) is null) return null;
        if (command.CategoryId is { } category && await categories.GetByIdAsync(category, cancellationToken) is null) return null;
        var transaction = new Transaction(user.UserId ?? throw new UnauthorizedAccessException(), command.AccountId,
            command.CategoryId, null, command.Description, command.Amount, ParseType(command.Type), command.OccurredOn);
        await repository.AddAsync(transaction, cancellationToken);
        return TransactionDto.From(transaction);
    }
}
