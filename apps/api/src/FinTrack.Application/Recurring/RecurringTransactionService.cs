using FinTrack.Application.Accounts;
using FinTrack.Application.Authentication;
using FinTrack.Application.Categories;
using FinTrack.Domain.Recurring;
using FinTrack.Domain.Transactions;

namespace FinTrack.Application.Recurring;

public sealed class RecurringTransactionService(IRecurringTransactionRepository repository,
    IAccountRepository accounts, ICategoryRepository categories, ICurrentUser user, TimeProvider clock)
{
    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public static Dictionary<string, string[]> Validate(CreateRecurringTransactionCommand command)
    {
        var type = command.Type switch { "Income" => TransactionType.Income, "Expense" => TransactionType.Expense, _ => (TransactionType)0 };
        var errors = RecurringTransaction.Validate(command.Description, command.Amount, type, command.StartDate, command.EndDate);
        if (command.AccountId == Guid.Empty) errors["accountId"] = ["Conta obrigatória."];
        if (command.CategoryId == Guid.Empty) errors["categoryId"] = ["Categoria inválida."];
        return errors;
    }
    public Dictionary<string, string[]> ValidateThroughDate(DateOnly date)
    {
        var errors = new Dictionary<string, string[]>();
        if (date == default || date > Today || date > RecurringTransaction.MaximumDate)
            errors["throughDate"] = ["Data de geração deve ser válida e não pode estar no futuro."];
        return errors;
    }
    public async Task<RecurringTransactionDto?> CreateAsync(CreateRecurringTransactionCommand command, CancellationToken cancellationToken)
    {
        if (await accounts.GetByIdAsync(command.AccountId, cancellationToken) is null) return null;
        if (command.CategoryId is { } categoryId && await categories.GetByIdAsync(categoryId, cancellationToken) is null) return null;
        var item = new RecurringTransaction(user.UserId ?? throw new UnauthorizedAccessException(), command.AccountId,
            command.CategoryId, command.Description, command.Amount,
            command.Type switch { "Income" => TransactionType.Income, "Expense" => TransactionType.Expense, _ => throw new ArgumentException("Tipo inválido.", nameof(command.Type)) }, command.StartDate, command.EndDate);
        await repository.AddAsync(item, cancellationToken);
        return RecurringTransactionDto.From(item);
    }
    public async Task<IReadOnlyList<RecurringTransactionDto>> ListAsync(CancellationToken cancellationToken)
        => (await repository.ListAsync(cancellationToken)).Select(RecurringTransactionDto.From).ToList();
    public async Task<RecurringTransactionDto?> GetAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetAsync(id, cancellationToken) is { } item ? RecurringTransactionDto.From(item) : null;
    public async Task<RecurringTransactionDto?> DeactivateAsync(Guid id, CancellationToken cancellationToken)
        => await repository.DeactivateAsync(id, cancellationToken) is { } item ? RecurringTransactionDto.From(item) : null;
    public Task<RecurringGenerationResult> GenerateAsync(DateOnly throughDate, CancellationToken cancellationToken)
    {
        if (ValidateThroughDate(throughDate).Count > 0) throw new ArgumentOutOfRangeException(nameof(throughDate));
        return repository.GenerateAsync(throughDate, cancellationToken);
    }
    public Task<IReadOnlyList<TransactionDto>> ListTransactionsAsync(CancellationToken cancellationToken)
        => repository.ListTransactionsAsync(cancellationToken);
}
