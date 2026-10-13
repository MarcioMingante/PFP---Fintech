using FinTrack.Domain.Recurring;
using FinTrack.Domain.Transactions;
using FinTrack.Application.Recurring;

namespace FinTrack.UnitTests.Domain.Recurring;

public sealed class RecurringTransactionTests
{
    private static RecurringTransaction Rule(DateOnly start, DateOnly? end = null, TransactionType type = TransactionType.Expense)
        => new(Guid.NewGuid(), Guid.NewGuid(), null, "  Netflix  ", 59.90m, type, start, end);

    [Theory]
    [InlineData(2024, 29)]
    [InlineData(2025, 28)]
    public void MonthlySchedule_RestoresAnchorAfterFebruary(int year, int februaryDay)
    {
        var rule = Rule(new(year, 1, 31));
        var january = rule.GenerateNext();
        var february = rule.GenerateNext();
        var march = rule.GenerateNext();
        var april = rule.GenerateNext();
        Assert.Equal(new DateOnly(year, 1, 31), january.OccurredOn);
        Assert.Equal(new DateOnly(year, 2, februaryDay), february.OccurredOn);
        Assert.Equal(new DateOnly(year, 3, 31), march.OccurredOn);
        Assert.Equal(new DateOnly(year, 4, 30), april.OccurredOn);
        Assert.Equal(new DateOnly(year, 5, 31), rule.NextOccurrenceDate);
        Assert.Equal(4, new[] { january.Id, february.Id, march.Id, april.Id }.Distinct().Count());
    }

    [Theory]
    [InlineData(TransactionType.Income)]
    [InlineData(TransactionType.Expense)]
    public void GeneratedTransaction_PreservesOwnerReferencesAndMoney(TransactionType type)
    {
        var category = Guid.NewGuid();
        var rule = new RecurringTransaction(Guid.NewGuid(), Guid.NewGuid(), category, "  Salário  ", 4100.01m, type, new(2025, 12, 5), null);
        var item = rule.GenerateNext();
        Assert.Equal(rule.Id, item.RecurringTransactionId);
        Assert.Equal(rule.UserId, item.UserId);
        Assert.Equal(rule.AccountId, item.AccountId);
        Assert.Equal(category, item.CategoryId);
        Assert.Equal("Salário", item.Description);
        Assert.Equal(4100.01m, item.Amount);
        Assert.Equal(type, item.Type);
        Assert.Equal(new DateOnly(2026, 1, 5), rule.NextOccurrenceDate);
    }

    [Fact]
    public void EndDate_IsInclusiveAndStopsGeneration()
    {
        var rule = Rule(new(2024, 1, 31), new(2024, 2, 29));
        rule.GenerateNext();
        Assert.True(rule.IsActive);
        rule.GenerateNext();
        Assert.False(rule.IsActive);
        Assert.Null(rule.NextOccurrenceDate);
        Assert.Throws<InvalidOperationException>(() => rule.GenerateNext());
        var single = Rule(new(2024, 1, 1), new(2024, 1, 1));
        single.GenerateNext();
        Assert.False(single.IsActive);
    }

    [Fact]
    public void Deactivate_IsIdempotentAndPreservesHistoryAndCheckpoint()
    {
        var rule = Rule(new(2024, 1, 1));
        var first = rule.GenerateNext();
        rule.Deactivate(); rule.Deactivate();
        Assert.False(rule.IsActive);
        Assert.Equal(new DateOnly(2024, 2, 1), rule.NextOccurrenceDate);
        Assert.Equal(new DateOnly(2024, 1, 1), first.OccurredOn);
        Assert.Throws<InvalidOperationException>(() => rule.GenerateNext());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000000000")]
    public void InvalidMoney_IsRejected(string amount)
    {
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentException>(() => new RecurringTransaction(Guid.NewGuid(), Guid.NewGuid(), null, "Compra", value, TransactionType.Expense, new(2024, 1, 1), null));
    }

    [Fact]
    public void RequiredFieldsDatesAndType_AreEnforced()
    {
        var errors = RecurringTransactionService.Validate(new(Guid.Empty, Guid.Empty, " ", 0m, "Transfer", default, DateOnly.MaxValue));
        foreach (var field in new[] { "accountId", "categoryId", "description", "amount", "type", "startDate", "endDate" }) Assert.Contains(field, errors.Keys);
        Assert.Throws<ArgumentException>(() => Rule(new(2024, 1, 1), new(2023, 12, 31)));
        Assert.Throws<ArgumentException>(() => Rule(default));
        Assert.Throws<ArgumentException>(() => Rule(DateOnly.MaxValue));
        Assert.Throws<ArgumentException>(() => Rule(new(2024, 1, 1), null, (TransactionType)0));
        Assert.Throws<ArgumentException>(() => new RecurringTransaction(Guid.NewGuid(), Guid.NewGuid(), null, new string('x', 201), 1m, TransactionType.Income, new(2024, 1, 1), null));
        var maximum = Rule(RecurringTransaction.MaximumDate);
        maximum.GenerateNext();
        Assert.False(maximum.IsActive);
        Assert.Null(maximum.NextOccurrenceDate);
    }
}
