namespace FinTrack.Domain.Purchases;

public static class InstallmentSchedule
{
    public static DateOnly DayInMonth(DateOnly month, int day)
        => new(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));

    public static IReadOnlyList<Installment> Generate(Guid purchaseId, decimal amount, int count,
        DateOnly purchaseDate, int closingDay, int dueDay)
    {
        var errors = Purchase.Validate("Compra", amount, count, purchaseDate, Guid.NewGuid());
        if (errors.Count > 0) throw new ArgumentException(errors.First().Value[0], errors.First().Key);
        if (closingDay is < 1 or > 31 || dueDay is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(closingDay));
        var month = new DateOnly(purchaseDate.Year, purchaseDate.Month, 1);
        if (purchaseDate >= DayInMonth(month, closingDay)) month = month.AddMonths(1);
        var cents = amount * 100m;
        var baseCents = decimal.Floor(cents / count);
        var remainder = (int)(cents % count);
        var installments = new List<Installment>(count);
        for (var i = 0; i < count; i++)
        {
            var cycle = month.AddMonths(i);
            var closing = DayInMonth(cycle, closingDay);
            var due = DayInMonth(cycle, dueDay);
            if (due <= closing) due = DayInMonth(cycle.AddMonths(1), dueDay);
            installments.Add(new Installment(purchaseId, i + 1,
                (baseCents + (i < remainder ? 1 : 0)) / 100m, closing, due));
        }
        return installments;
    }
}
