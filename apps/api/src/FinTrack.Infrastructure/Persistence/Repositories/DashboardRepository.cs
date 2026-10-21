using System.Data;
using FinTrack.Application.Dashboard;
using FinTrack.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository(FinTrackDbContext database) : IDashboardRepository
{
    public async Task<DashboardSummaryDto> SummaryAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        // All read models share the same database snapshot. No rows are created by GET.
        await using var snapshot = await database.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var month = new DateOnly(asOf.Year, asOf.Month, 1);
        var historyStart = month.AddMonths(-11);
        var futureStart = month.AddMonths(1);
        var futureEnd = month.AddMonths(4).AddDays(-1);
        var totals = await database.Transactions.Where(item => item.OccurredOn <= asOf)
            .GroupBy(item => item.Type).Select(group => new { Type = group.Key, Amount = group.Sum(item => item.Amount) })
            .ToListAsync(cancellationToken);
        var historyRows = await database.Transactions.Where(item => item.OccurredOn >= historyStart && item.OccurredOn <= asOf)
            .GroupBy(item => new { item.OccurredOn.Year, item.OccurredOn.Month, item.Type })
            .Select(group => new { group.Key.Year, group.Key.Month, group.Key.Type, Amount = group.Sum(item => item.Amount) })
            .ToListAsync(cancellationToken);
        var history = Enumerable.Range(0, 12).Select(offset =>
        {
            var period = historyStart.AddMonths(offset);
            var rows = historyRows.Where(row => row.Year == period.Year && row.Month == period.Month);
            return new MonthlyCashFlowDto(period, rows.Where(row => row.Type == TransactionType.Income).Sum(row => row.Amount),
                rows.Where(row => row.Type == TransactionType.Expense).Sum(row => row.Amount));
        }).ToList();
        var categories = await (
            from item in database.Transactions
            join category in database.Categories on item.CategoryId equals category.Id into categoryGroup
            from category in categoryGroup.DefaultIfEmpty()
            where item.Type == TransactionType.Expense && item.OccurredOn >= month && item.OccurredOn <= asOf
            group item by new { item.CategoryId, Name = category == null ? "Sem categoria" : category.Name } into grouped
            orderby grouped.Sum(item => item.Amount) descending
            select new CategoryExpenseDto(grouped.Key.CategoryId, grouped.Key.Name, grouped.Sum(item => item.Amount))
        ).ToListAsync(cancellationToken);
        var invoices = await (
            from invoice in database.CreditCardInvoices
            join card in database.CreditCards on invoice.CreditCardId equals card.Id
            join installment in database.Set<FinTrack.Domain.Purchases.Installment>() on invoice.Id equals installment.InvoiceId
            where invoice.DueDate >= month && invoice.DueDate < futureStart
            group installment by new { invoice.Id, invoice.CreditCardId, card.Name, invoice.ClosingDate, invoice.DueDate } into grouped
            orderby grouped.Key.DueDate, grouped.Key.Id
            select new DashboardInvoiceDto(grouped.Key.Id, grouped.Key.CreditCardId, grouped.Key.Name,
                grouped.Key.ClosingDate, grouped.Key.DueDate, grouped.Sum(item => item.Amount))
        ).ToListAsync(cancellationToken);
        var futureInstallments = await database.Set<FinTrack.Domain.Purchases.Installment>()
            .Where(item => item.DueDate >= futureStart && item.DueDate <= futureEnd)
            .GroupBy(item => new { item.DueDate.Year, item.DueDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Total = group.Sum(item => item.Amount) }).ToListAsync(cancellationToken);
        var recordedFutureExpenses = await database.Transactions
            .Where(item => item.Type == TransactionType.Expense && item.OccurredOn >= futureStart && item.OccurredOn <= futureEnd)
            .GroupBy(item => new { item.OccurredOn.Year, item.OccurredOn.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Total = group.Sum(item => item.Amount) }).ToListAsync(cancellationToken);
        var recurring = await database.RecurringTransactions.AsNoTracking()
            .Where(item => item.IsActive && item.Type == TransactionType.Expense && item.StartDate <= futureEnd
                && (item.EndDate == null || item.EndDate >= futureStart))
            .Select(item => new { item.StartDate, item.EndDate, item.NextOccurrenceDate, item.Amount }).ToListAsync(cancellationToken);
        var forecast = Enumerable.Range(0, 3).Select(offset =>
        {
            var period = futureStart.AddMonths(offset);
            var recurringTotal = recurring.Where(item =>
            {
                var date = new DateOnly(period.Year, period.Month, Math.Min(item.StartDate.Day, DateTime.DaysInMonth(period.Year, period.Month)));
                return date >= item.StartDate && date >= item.NextOccurrenceDate && (item.EndDate == null || date <= item.EndDate);
            }).Sum(item => item.Amount) + recordedFutureExpenses.Where(item => item.Year == period.Year && item.Month == period.Month).Sum(item => item.Total);
            var installments = futureInstallments.Where(item => item.Year == period.Year && item.Month == period.Month).Sum(item => item.Total);
            return new FutureExpenseDto(period, recurringTotal, installments, recurringTotal + installments);
        }).ToList();
        var balance = totals.Where(item => item.Type == TransactionType.Income).Sum(item => item.Amount)
            - totals.Where(item => item.Type == TransactionType.Expense).Sum(item => item.Amount);
        await snapshot.CommitAsync(cancellationToken);
        return new(asOf, balance, history[^1].Income, history[^1].Expenses, invoices.Sum(item => item.Total),
            forecast.Sum(item => item.Total), categories, history, invoices, forecast);
    }
}
