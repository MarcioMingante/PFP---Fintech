using FinTrack.Domain.CreditCards;
using FinTrack.Domain.Purchases;

namespace FinTrack.UnitTests.Domain.Purchases;

public sealed class PurchaseTests
{
    private static CreditCard Card(int closing = 25, int due = 5)
    {
        var card = new CreditCard("Principal", 5000m, closing, due);
        card.AssignOwner(Guid.NewGuid());
        return card;
    }

    [Theory]
    [InlineData(2026, 1, 24, 2026, 1, 25, 2026, 2, 5)]
    [InlineData(2026, 1, 25, 2026, 2, 25, 2026, 3, 5)]
    [InlineData(2026, 1, 26, 2026, 2, 25, 2026, 3, 5)]
    [InlineData(2026, 12, 31, 2027, 1, 25, 2027, 2, 5)]
    public void Calendar_AssignsFirstCycle(int y, int m, int d, int cy, int cm, int cd, int dy, int dm, int dd)
    {
        var purchase = new Purchase(Card(), "Compra", 1200m, 4, new(y, m, d), Guid.NewGuid());
        var items = purchase.Installments.ToArray();
        Assert.Equal(4, items.Length);
        Assert.Equal(new DateOnly(cy, cm, cd), items[0].ClosingDate);
        Assert.Equal(new DateOnly(dy, dm, dd), items[0].DueDate);
        Assert.Equal(new DateOnly(dy, dm, dd).AddMonths(3), items[3].DueDate);
        Assert.All(items, item => Assert.Equal(300m, item.Amount));
        Assert.Equal(new[] { 1, 2, 3, 4 }, items.Select(item => item.Number));
    }

    [Fact]
    public void Remainder_IsDistributedWithoutLosingCents()
    {
        var purchase = new Purchase(Card(), "  Compra  ", 100m, 3, new(2026, 1, 1), Guid.NewGuid());
        Assert.Equal(new[] { 33.34m, 33.33m, 33.33m }, purchase.Installments.Select(item => item.Amount));
        Assert.Equal(100m, purchase.Installments.Sum(item => item.Amount));
        Assert.Equal("Compra", purchase.Description);
        Assert.All(purchase.Installments, item => Assert.Equal(purchase.Id, item.PurchaseId));
        var tiny = new Purchase(Card(), "Centavos", .05m, 3, new(2026, 1, 1), Guid.NewGuid());
        Assert.Equal(new[] { .02m, .02m, .01m }, tiny.Installments.Select(item => item.Amount));
    }

    [Theory]
    [InlineData(2024, 2, 28, 31, 31, 2024, 2, 29, 2024, 3, 31)]
    [InlineData(2024, 2, 29, 31, 31, 2024, 3, 31, 2024, 4, 30)]
    [InlineData(2025, 2, 28, 31, 5, 2025, 3, 31, 2025, 4, 5)]
    [InlineData(2026, 1, 1, 10, 20, 2026, 1, 10, 2026, 1, 20)]
    public void Calendar_HandlesShortMonthsLeapYearsAndSameMonthDue(int y, int m, int d, int closing, int due,
        int cy, int cm, int cd, int dy, int dm, int dd)
    {
        var item = new Purchase(Card(closing, due), "Compra", 1m, 1, new(y, m, d), Guid.NewGuid()).Installments.Single();
        Assert.Equal(new DateOnly(cy, cm, cd), item.ClosingDate);
        Assert.Equal(new DateOnly(dy, dm, dd), item.DueDate);
    }

    [Fact]
    public void Schedule_RecomputesEachMonthAndPreservesSnapshot()
    {
        var card = Card(31, 31);
        var purchase = new Purchase(card, "Compra", 3m, 3, new(2024, 1, 1), Guid.NewGuid());
        card.Update("Mudou", 2000m, 10, 20);
        Assert.Equal(new[] { new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 31), new DateOnly(2024, 4, 30) },
            purchase.Installments.Select(item => item.DueDate));
        Assert.Equal(31, purchase.ClosingDay);
        Assert.Equal(31, purchase.DueDay);
    }

    [Theory]
    [InlineData("0", 1)]
    [InlineData("-1", 1)]
    [InlineData("1.001", 1)]
    [InlineData("0.01", 2)]
    [InlineData("100", 0)]
    [InlineData("100", 121)]
    [InlineData("10000000000000000", 1)]
    public void InvalidAmountOrCount_IsRejected(string amount, int count)
    {
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentException>(() => new Purchase(Card(), "Compra", value, count, new(2026, 1, 1), Guid.NewGuid()));
    }

    [Fact]
    public void RequiredFieldsAndCalendarRange_AreEnforced()
    {
        var card = Card();
        Assert.Throws<ArgumentException>(() => new Purchase(card, " ", 1m, 1, new(2026, 1, 1), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Purchase(card, new string('x', 201), 1m, 1, new(2026, 1, 1), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Purchase(card, "Compra", 1m, 1, default, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Purchase(card, "Compra", 1m, 1, DateOnly.MaxValue, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Purchase(card, "Compra", 1m, 1, new(2026, 1, 1), Guid.Empty));
        var maximum = new Purchase(card, "Máximo", CreditCardRules.MaximumLimit, 120, new(9989, 1, 1), Guid.NewGuid());
        Assert.Equal(CreditCardRules.MaximumLimit, maximum.Installments.Sum(item => item.Amount));
    }
}
