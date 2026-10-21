using FinTrack.Application.Accounts;
using FinTrack.Application.Authentication;
using FinTrack.Domain.Transfers;

namespace FinTrack.Application.Transfers;

public sealed record CreateTransferCommand(Guid SourceAccountId, Guid DestinationAccountId,
    string Description, decimal Amount, DateOnly OccurredOn, Guid IdempotencyKey);
public sealed record TransferDto(Guid Id, Guid SourceAccountId, Guid DestinationAccountId,
    string Description, decimal Amount, DateOnly OccurredOn, Guid IdempotencyKey)
{
    public static TransferDto From(Transfer transfer) => new(transfer.Id, transfer.SourceAccountId,
        transfer.DestinationAccountId, transfer.Description, transfer.Amount, transfer.OccurredOn, transfer.IdempotencyKey);
}
public sealed record AccountBalanceDto(Guid Id, string Name, decimal Balance);
public interface ITransferRepository
{
    Task<Transfer?> FindAsync(Guid key, CancellationToken cancellationToken);
    Task<Transfer?> AddOrGetAsync(Transfer transfer, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransferDto>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountBalanceDto>> BalancesAsync(CancellationToken cancellationToken);
}
public enum TransferStatus { Created, Replayed, NotFound, Conflict }
public sealed record TransferResult(TransferStatus Status, TransferDto? Transfer = null);
public sealed class TransferService(ITransferRepository transfers, IAccountRepository accounts, ICurrentUser user, TimeProvider clock)
{
    public Dictionary<string, string[]> Validate(CreateTransferCommand command)
    {
        var errors = Transfer.Validate(command.SourceAccountId, command.DestinationAccountId, command.Description,
            command.Amount, command.OccurredOn, command.IdempotencyKey);
        if (command.OccurredOn > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            errors["occurredOn"] = ["Transferência não pode ter data futura."];
        return errors;
    }
    public async Task<TransferResult> CreateAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        if (Validate(command).Count > 0) throw new ArgumentException("Transferência inválida.");
        var existing = await transfers.FindAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null) return Replay(existing, command);
        if (await accounts.GetByIdAsync(command.SourceAccountId, cancellationToken) is null ||
            await accounts.GetByIdAsync(command.DestinationAccountId, cancellationToken) is null)
            return new(TransferStatus.NotFound);
        var transfer = new Transfer(user.UserId ?? throw new UnauthorizedAccessException(), command.SourceAccountId,
            command.DestinationAccountId, command.Description, command.Amount, command.OccurredOn, command.IdempotencyKey);
        var saved = await transfers.AddOrGetAsync(transfer, cancellationToken);
        return saved is null ? new(TransferStatus.NotFound) : saved.Id == transfer.Id
            ? new(TransferStatus.Created, TransferDto.From(saved)) : Replay(saved, command);
    }
    private static TransferResult Replay(Transfer transfer, CreateTransferCommand command) =>
        transfer.SourceAccountId == command.SourceAccountId && transfer.DestinationAccountId == command.DestinationAccountId &&
        transfer.Description == command.Description.Trim() && transfer.Amount == command.Amount && transfer.OccurredOn == command.OccurredOn
            ? new(TransferStatus.Replayed, TransferDto.From(transfer)) : new(TransferStatus.Conflict);
}
