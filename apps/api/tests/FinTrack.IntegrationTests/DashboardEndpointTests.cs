using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Dashboard;
using FinTrack.Application.Recurring;
using FinTrack.Application.Purchases;
using FinTrack.IntegrationTests.Fixtures;

namespace FinTrack.IntegrationTests;

public sealed class DashboardEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private sealed record Resource(Guid Id, string Name);
    private static async Task<Guid> ResourceId(HttpClient client, string route, object request)
    {
        var response = await client.PostAsJsonAsync(route, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Resource>())!.Id;
    }
    private static async Task Recurring(HttpClient client, Guid account, Guid? category, string type, decimal amount, DateOnly start, DateOnly? end)
    {
        var response = await client.PostAsJsonAsync("/recurring-transactions", new CreateRecurringTransactionCommand(account, category, type, amount, type, start, end));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [PostgresFact]
    public async Task Summary_AggregatesCashFlowCategoriesInvoicesAndFutureCommitments()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var account = await ResourceId(client, "/accounts", new { Name = "Conta" });
        var category = await ResourceId(client, "/categories", new { Name = "Assinaturas" });
        await Recurring(client, account, null, "Income", 1000m, new(2024, 1, 5), null);
        await Recurring(client, account, category, "Expense", 59.90m, new(2024, 1, 31), new(2024, 6, 30));
        await Recurring(client, account, null, "Expense", 10.01m, new(2024, 3, 1), new(2024, 3, 1));
        var generation = await client.PostAsJsonAsync("/recurring-transactions/generate", new GenerateRecurringTransactionsRequest(new(2024, 4, 30)));
        Assert.Equal(HttpStatusCode.OK, generation.StatusCode);
        var card = await ResourceId(client, "/credit-cards", new { Name = "Cartão", Limit = 5000m, ClosingDay = 25, DueDay = 5 });
        var purchase = await client.PostAsJsonAsync($"/credit-cards/{card}/purchases", new CreatePurchaseCommand("Compra", 100m, 3, new(2024, 1, 24), Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, purchase.StatusCode);
        var result = (await client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2024-03-31"))!;
        Assert.Equal(2810.29m, result.CurrentBalance);
        Assert.Equal(1000m, result.IncomeThisMonth);
        Assert.Equal(69.91m, result.ExpensesThisMonth);
        Assert.Equal(33.33m, result.CreditCardInvoicesThisMonth);
        Assert.Equal(213.03m, result.FutureExpenses); // Recorded April expense + May/June projections + April installment.
        Assert.Equal(12, result.MonthlyHistory.Count);
        Assert.Equal(new DateOnly(2023, 4, 1), result.MonthlyHistory[0].Month);
        Assert.Equal(0m, result.MonthlyHistory[0].Income);
        Assert.Equal(1000m, result.MonthlyHistory[^1].Income);
        Assert.Equal(59.90m, Assert.Single(result.ExpensesByCategory, item => item.CategoryId == category).Total);
        Assert.Equal(10.01m, Assert.Single(result.ExpensesByCategory, item => item.CategoryId == null).Total);
        Assert.Equal("Sem categoria", Assert.Single(result.ExpensesByCategory, item => item.CategoryId == null).Name);
        Assert.Equal(new DateOnly(2024, 3, 5), Assert.Single(result.Invoices).DueDate);
        Assert.Equal(33.33m, result.Forecast[0].Installments);
        Assert.Equal(59.90m, result.Forecast[0].RecurringExpenses); // Already generated occurrences are not projected twice.
        Assert.Equal(59.90m, result.Forecast[1].RecurringExpenses);
        Assert.Equal(59.90m, result.Forecast[2].RecurringExpenses);
        var midMonth = (await client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2024-03-15"))!;
        Assert.Equal(10.01m, midMonth.ExpensesThisMonth);
        Assert.Equal(2870.19m, midMonth.CurrentBalance);
        Assert.Equal(0m, (await client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2023-12-31"))!.CurrentBalance);
    }

    [PostgresFact]
    public async Task Summary_IsEmptyForOtherUsersAndRequiresAuthentication()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        var account = await ResourceId(owner, "/accounts", new { Name = "Privada" });
        await Recurring(owner, account, null, "Expense", 1m, new(2024, 1, 1), null);
        await owner.PostAsJsonAsync("/recurring-transactions/generate", new GenerateRecurringTransactionsRequest(new(2024, 1, 1)));
        using var other = await fixture.CreateAuthenticatedClientAsync();
        var empty = (await other.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2024-01-31"))!;
        Assert.Equal(0m, empty.CurrentBalance);
        Assert.Equal(0m, empty.FutureExpenses);
        Assert.Empty(empty.Invoices);
        Assert.Empty(empty.ExpensesByCategory);
        Assert.All(empty.MonthlyHistory, month => Assert.Equal((0m, 0m), (month.Income, month.Expenses)));
        Assert.Equal(3, empty.Forecast.Count);
        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/dashboard/summary")).StatusCode);
        foreach (var date in new[] { "0001-01-01", "9999-12-31", "invalid" })
            Assert.Equal(HttpStatusCode.BadRequest, (await other.GetAsync($"/dashboard/summary?asOf={date}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/dashboard/summary")).StatusCode);
    }
}
