using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Recurring;
using FinTrack.Application.Dashboard;
using FinTrack.Application.Transactions;
using FinTrack.IntegrationTests.Fixtures;

namespace FinTrack.IntegrationTests;

public sealed class ManualTransactionEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private sealed record Account(Guid Id, string Name);
    private static async Task<Guid> CreateAccount(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/accounts", new { Name = "Conta manual" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Account>())!.Id;
    }
    [Fact]
    public async Task ManualIncomeAndExpense_PersistAndFeedDashboardWithoutRecurringSource()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var account = await CreateAccount(client);
        foreach (var type in new[] { "Income", "Expense" })
        {
            var response = await client.PostAsJsonAsync("/transactions", new CreateTransactionCommand(account, null, "  Manual  ", type == "Income" ? 100m : 10.01m, type, new(2024, 3, 5)));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var item = (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
            Assert.Null(item.RecurringTransactionId);
            Assert.Equal("Manual", item.Description);
        }
        var transactions = (await client.GetFromJsonAsync<TransactionDto[]>("/transactions"))!;
        Assert.Equal(2, transactions.Length);
        var summary = (await client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2024-03-31"))!;
        Assert.Equal(89.99m, summary.CurrentBalance);
        Assert.Equal(100m, summary.IncomeThisMonth);
        Assert.Equal(10.01m, summary.ExpensesThisMonth);
    }
    [Fact]
    public async Task OwnershipValidationAndFutureDates_PreventInvalidWrites()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        using var other = await fixture.CreateAuthenticatedClientAsync();
        using var anonymous = fixture.CreateClient();
        var account = await CreateAccount(owner);
        var valid = new CreateTransactionCommand(account, null, "Manual", 1m, "Expense", new(2024, 1, 1));
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync("/transactions", valid)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/transactions", valid)).StatusCode);
        foreach (var invalid in new[] { valid with { Amount = .001m }, valid with { Type = "Transfer" }, valid with { OccurredOn = DateOnly.MaxValue }, valid with { OccurredOn = default }, valid with { AccountId = Guid.Empty }, valid with { CategoryId = Guid.Empty } })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/transactions", invalid)).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<TransactionDto[]>("/transactions"))!);
    }
}
