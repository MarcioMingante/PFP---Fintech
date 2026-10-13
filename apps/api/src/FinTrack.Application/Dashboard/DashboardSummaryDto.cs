namespace FinTrack.Application.Dashboard;

public sealed record MonthlyCashFlowDto(DateOnly Month, decimal Income, decimal Expenses);
public sealed record CategoryExpenseDto(Guid? CategoryId, string Name, decimal Total);
public sealed record DashboardInvoiceDto(Guid Id, Guid CreditCardId, string CardName, DateOnly ClosingDate, DateOnly DueDate, decimal Total);
public sealed record FutureExpenseDto(DateOnly Month, decimal RecurringExpenses, decimal Installments, decimal Total);
public sealed record DashboardSummaryDto(DateOnly AsOf, decimal CurrentBalance, decimal IncomeThisMonth,
    decimal ExpensesThisMonth, decimal CreditCardInvoicesThisMonth, decimal FutureExpenses,
    IReadOnlyList<CategoryExpenseDto> ExpensesByCategory, IReadOnlyList<MonthlyCashFlowDto> MonthlyHistory,
    IReadOnlyList<DashboardInvoiceDto> Invoices, IReadOnlyList<FutureExpenseDto> Forecast);

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> SummaryAsync(DateOnly asOf, CancellationToken cancellationToken);
}
public sealed class DashboardService(IDashboardRepository repository, TimeProvider clock)
{
    public static Dictionary<string, string[]> Validate(DateOnly? date)
    {
        var errors = new Dictionary<string, string[]>();
        if (date is { } value && (value < new DateOnly(2, 1, 1) || value > new DateOnly(9998, 12, 1)))
            errors["asOf"] = ["Data deve estar entre 0002-01-01 e 9998-12-01."];
        return errors;
    }
    public Task<DashboardSummaryDto> SummaryAsync(DateOnly? asOf, CancellationToken cancellationToken)
    {
        var date = asOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (Validate(date).Count > 0) throw new ArgumentOutOfRangeException(nameof(asOf));
        return repository.SummaryAsync(date, cancellationToken);
    }
}
