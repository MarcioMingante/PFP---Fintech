using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Recurring;
using FinTrack.IntegrationTests.Fixtures;

namespace FinTrack.IntegrationTests;

public sealed class RecurringTransactionEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private const string Route = "/recurring-transactions";
    private sealed record Resource(Guid Id, string Name);
    private static async Task<Guid> Account(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/accounts", new { Name = "Corrente" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Resource>())!.Id;
    }
    private static CreateRecurringTransactionCommand Command(Guid account, DateOnly? start = null, DateOnly? end = null, string type = "Expense", Guid? category = null)
        => new(account, category, "  Mensalidade  ", type == "Income" ? 4100m : 59.90m, type, start ?? new(2024, 1, 31), end);
    private static async Task<RecurringTransactionDto> Create(HttpClient client, CreateRecurringTransactionCommand command)
    {
        var response = await client.PostAsJsonAsync(Route, command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<RecurringTransactionDto>())!;
        Assert.Equal($"{Route}/{item.Id}", response.Headers.Location?.OriginalString);
        return item;
    }
    private static async Task<RecurringGenerationResult> Generate(HttpClient client, DateOnly? throughDate)
    {
        var response = await client.PostAsJsonAsync($"{Route}/generate", new GenerateRecurringTransactionsRequest(throughDate));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RecurringGenerationResult>())!;
    }

    [Fact]
    public async Task MonthlyGeneration_PersistsIncomeExpenseAndRecoversMissedMonths()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var account = await Account(client);
        var categoryResponse = await client.PostAsJsonAsync("/categories", new { Name = "Assinaturas" });
        var category = (await categoryResponse.Content.ReadFromJsonAsync<Resource>())!.Id;
        var expense = await Create(client, Command(account, end: new(2024, 3, 31), category: category));
        var income = await Create(client, Command(account, start: new(2024, 1, 5), end: new(2024, 3, 5), type: "Income"));
        Assert.Equal("Monthly", expense.Frequency);
        Assert.Equal("Mensalidade", expense.Description);
        Assert.Equal(expense, await client.GetFromJsonAsync<RecurringTransactionDto>($"{Route}/{expense.Id}"));
        Assert.Equal(2, (await client.GetFromJsonAsync<RecurringTransactionDto[]>(Route))!.Length);
        Assert.Empty((await client.GetFromJsonAsync<TransactionDto[]>("/transactions"))!);
        Assert.Equal(0, (await Generate(client, new(2024, 1, 4))).GeneratedCount);
        var generated = await Generate(client, new(2024, 3, 31));
        Assert.Equal(6, generated.GeneratedCount);
        Assert.False(generated.HasMore);
        Assert.Equal(0, (await Generate(client, new(2024, 3, 31))).GeneratedCount);
        using var reader = fixture.CreateClient();
        reader.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        var transactions = (await reader.GetFromJsonAsync<TransactionDto[]>("/transactions"))!;
        Assert.Equal(6, transactions.Length);
        Assert.Equal(12300m, transactions.Where(item => item.Type == "Income").Sum(item => item.Amount));
        Assert.Equal(179.70m, transactions.Where(item => item.Type == "Expense").Sum(item => item.Amount));
        var expenses = transactions.Where(item => item.RecurringTransactionId == expense.Id).OrderBy(item => item.OccurredOn).ToArray();
        Assert.Equal(new[] { new DateOnly(2024, 1, 31), new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 31) }, expenses.Select(item => item.OccurredOn));
        Assert.All(expenses, item => Assert.Equal(category, item.CategoryId));
        Assert.All(transactions, item => Assert.Equal(account, item.AccountId));
        Assert.False((await reader.GetFromJsonAsync<RecurringTransactionDto>($"{Route}/{income.Id}"))!.IsActive);
        Assert.Null((await reader.GetFromJsonAsync<RecurringTransactionDto>($"{Route}/{expense.Id}"))!.NextOccurrenceDate);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/accounts/{account}")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentGeneration_ProducesEachOccurrenceOnlyOnce()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var account = await Account(client);
        await Create(client, Command(account, end: new(2024, 3, 31)));
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Generate(client, new(2024, 3, 31))));
        Assert.Equal(3, results.Sum(result => result.GeneratedCount));
        var transactions = (await client.GetFromJsonAsync<TransactionDto[]>("/transactions"))!;
        Assert.Equal(3, transactions.Length);
        Assert.Equal(3, transactions.Select(item => (item.RecurringTransactionId, item.OccurredOn)).Distinct().Count());
    }

    [Fact]
    public async Task BoundedCatchUp_ContinuesFromCheckpoint()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var account = await Account(client);
        var rule = await Create(client, Command(account, start: new(2000, 1, 31), end: new(2010, 12, 31)));
        var first = await Generate(client, new(2010, 12, 31));
        Assert.Equal(120, first.GeneratedCount);
        Assert.True(first.HasMore);
        Assert.Equal(new DateOnly(2010, 1, 31), (await client.GetFromJsonAsync<RecurringTransactionDto>($"{Route}/{rule.Id}"))!.NextOccurrenceDate);
        var second = await Generate(client, new(2010, 12, 31));
        Assert.Equal(12, second.GeneratedCount);
        Assert.False(second.HasMore);
        Assert.Equal(0, (await Generate(client, new(2010, 12, 31))).GeneratedCount);
        Assert.Equal(132, (await client.GetFromJsonAsync<TransactionDto[]>("/transactions"))!.Length);
    }

    [Fact]
    public async Task Deactivation_PreservesGeneratedHistoryAndStopsFutureWork()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var account = await Account(client);
        var rule = await Create(client, Command(account));
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/accounts/{account}")).StatusCode);
        Assert.Equal(1, (await Generate(client, new(2024, 1, 31))).GeneratedCount);
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var response = await client.PostAsync($"{Route}/{rule.Id}/deactivate", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False((await response.Content.ReadFromJsonAsync<RecurringTransactionDto>())!.IsActive);
        }
        Assert.Equal(0, (await Generate(client, new(2024, 12, 31))).GeneratedCount);
        Assert.Single((await client.GetFromJsonAsync<TransactionDto[]>("/transactions"))!);
        Assert.Equal(new DateOnly(2024, 2, 29), (await client.GetFromJsonAsync<RecurringTransactionDto>($"{Route}/{rule.Id}"))!.NextOccurrenceDate);
    }

    [Fact]
    public async Task Ownership_ProtectsRulesReferencesAndGeneratedTransactions()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        using var other = await fixture.CreateAuthenticatedClientAsync();
        using var anonymous = fixture.CreateClient();
        var account = await Account(owner);
        var rule = await Create(owner, Command(account));
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(Route, Command(account))).StatusCode);
        var otherAccount = await Account(other);
        var categoryResponse = await owner.PostAsJsonAsync("/categories", new { Name = "Privada" });
        var category = (await categoryResponse.Content.ReadFromJsonAsync<Resource>())!.Id;
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(Route, Command(otherAccount, category: category))).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<RecurringTransactionDto[]>(Route))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"{Route}/{rule.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"{Route}/{rule.Id}/deactivate", null)).StatusCode);
        Assert.Equal(0, (await Generate(other, new(2024, 3, 31))).GeneratedCount);
        Assert.Equal(3, (await Generate(owner, new(2024, 3, 31))).GeneratedCount);
        Assert.Empty((await other.GetFromJsonAsync<TransactionDto[]>("/transactions"))!);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Route}/{rule.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/transactions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Route, Command(account))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync($"{Route}/generate", new GenerateRecurringTransactionsRequest(null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync($"{Route}/{rule.Id}/deactivate", null)).StatusCode);
        Assert.True((await owner.GetFromJsonAsync<RecurringTransactionDto>($"{Route}/{rule.Id}"))!.IsActive);
    }

    [Fact]
    public async Task Validation_RejectsInvalidMoneyDatesTypeAndFutureGeneration()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var invalid = await client.PostAsJsonAsync(Route, new CreateRecurringTransactionCommand(Guid.Empty, Guid.Empty, " ", .001m, "Transfer", default, DateOnly.MaxValue));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var body = await invalid.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        foreach (var field in new[] { "accountId", "categoryId", "description", "amount", "type", "startDate", "endDate" })
            Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Empty((await client.GetFromJsonAsync<RecurringTransactionDto[]>(Route))!);
        foreach (var date in new[] { default(DateOnly), DateOnly.MaxValue })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"{Route}/generate", new GenerateRecurringTransactionsRequest(date))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Route, Command(Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"{Route}/{Guid.NewGuid()}/deactivate", null)).StatusCode);
        var account = await Account(client);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        await Create(client, Command(account, start: tomorrow));
        Assert.Equal(0, (await Generate(client, null)).GeneratedCount);
        Assert.Empty((await client.GetFromJsonAsync<TransactionDto[]>("/transactions"))!);
    }
}
