using FinTrack.Domain.CreditCards;
using FinTrack.Application.CreditCards.Commands.CreateCreditCard;
using FinTrack.Application.CreditCards.Commands.UpdateCreditCard;
using FinTrack.Application.CreditCards.Validators;

namespace FinTrack.UnitTests.Domain.CreditCards;

public sealed class CreditCardTests
{
    [Fact]
    public void ValidCard_TrimsNameAndPreservesDecimalPrecision()
    {
        var card = new CreditCard("  Principal  ", 1200.01m, 31, 1);
        Assert.NotEqual(Guid.Empty, card.Id);
        Assert.Equal("Principal", card.Name);
        Assert.Equal(1200.01m, card.Limit);
        Assert.Equal(31, card.ClosingDay);
        Assert.Equal(1, card.DueDay);
        card.Update("Novo", 9999999999999999.99m, 1, 31);
        Assert.Equal(CreditCardRules.MaximumLimit, card.Limit);
        Assert.Equal("Novo", card.Name);
    }

    [Theory]
    [InlineData("", "100", 10, 20, "name")]
    [InlineData(null, "100", 10, 20, "name")]
    [InlineData("Cartão", "0", 10, 20, "limit")]
    [InlineData("Cartão", "-1", 10, 20, "limit")]
    [InlineData("Cartão", "1.001", 10, 20, "limit")]
    [InlineData("Cartão", "10000000000000000", 10, 20, "limit")]
    [InlineData("Cartão", "100", 0, 20, "closingDay")]
    [InlineData("Cartão", "100", 32, 20, "closingDay")]
    [InlineData("Cartão", "100", 10, 0, "dueDay")]
    [InlineData("Cartão", "100", 10, 32, "dueDay")]
    public void InvalidValues_RejectCreationAndPreserveEntireCardOnUpdate(string? name, string amount, int closingDay, int dueDay, string field)
    {
        var limit = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentException>(() => new CreditCard(name!, limit, closingDay, dueDay));
        var card = new CreditCard("Original", 100m, 10, 20);
        Assert.Throws<ArgumentException>(() => card.Update(name!, limit, closingDay, dueDay));
        Assert.Equal(("Original", 100m, 10, 20), (card.Name, card.Limit, card.ClosingDay, card.DueDay));
        Assert.Contains(field, new CreateCreditCardCommandValidator().Validate(new(name!, limit, closingDay, dueDay)).Keys);
        Assert.Contains(field, new UpdateCreditCardCommandValidator().Validate(new(card.Id, name!, limit, closingDay, dueDay)).Keys);
    }

    [Fact]
    public void NameLengthAndOwner_AreEnforced()
    {
        Assert.Throws<ArgumentException>(() => new CreditCard(new string('a', 101), 100m, 10, 20));
        var card = new CreditCard(new string('a', 100), 100m, 10, 20);
        Assert.Throws<ArgumentException>(() => card.AssignOwner(Guid.Empty));
        var owner = Guid.NewGuid();
        card.AssignOwner(owner);
        card.AssignOwner(owner);
        Assert.Throws<InvalidOperationException>(() => card.AssignOwner(Guid.NewGuid()));
        Assert.Equal(owner, card.UserId);
    }
}
