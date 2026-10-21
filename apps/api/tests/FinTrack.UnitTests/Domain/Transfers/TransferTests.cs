using FinTrack.Domain.Transfers;

namespace FinTrack.UnitTests.Domain.Transfers;

public sealed class TransferTests
{
    [Fact]
    public void TransferPreservesBothSidesAndExactAmount()
    {
        var source = Guid.NewGuid(); var destination = Guid.NewGuid();
        var transfer = new Transfer(Guid.NewGuid(), source, destination, "  Transferência  ", 9999999999999999.99m, new(2024, 3, 5), Guid.NewGuid());
        Assert.Equal(source, transfer.SourceAccountId);
        Assert.Equal(destination, transfer.DestinationAccountId);
        Assert.Equal(9999999999999999.99m, transfer.Amount);
        Assert.Equal("Transferência", transfer.Description);
    }
    [Fact]
    public void InvalidSidesAmountsAndDatesAreRejected()
    {
        var account = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new Transfer(Guid.NewGuid(), account, account, "Mesmo destino", 1m, new(2024, 1, 1), Guid.NewGuid()));
        foreach (var amount in new[] { 0m, -1m, .001m, 10000000000000000m })
            Assert.Throws<ArgumentException>(() => new Transfer(Guid.NewGuid(), account, Guid.NewGuid(), "Inválida", amount, new(2024, 1, 1), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Transfer(Guid.NewGuid(), account, Guid.NewGuid(), "Data", 1m, default, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Transfer(Guid.NewGuid(), account, Guid.NewGuid(), "Chave", 1m, new(2024, 1, 1), Guid.Empty));
    }
}
