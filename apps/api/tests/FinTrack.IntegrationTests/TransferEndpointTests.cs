using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Transfers;
using FinTrack.Application.Dashboard;
using FinTrack.Infrastructure.Persistence;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.IntegrationTests;

public sealed class TransferEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private sealed record Account(Guid Id, string Name);
    private static async Task<Guid> AccountAsync(HttpClient client, string name)
        => (await (await client.PostAsJsonAsync("/accounts", new { Name = name })).Content.ReadFromJsonAsync<Account>())!.Id;
    private static CreateTransferCommand Command(Guid source, Guid destination, decimal amount = 10.01m)
        => new(source, destination, "Transferência", amount, new(2024, 3, 5), Guid.NewGuid());

    [Fact]
    public async Task TransfersMoveBalancesAtomicallyWithoutInflatingIncomeOrExpenses()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var source = await AccountAsync(client, "Origem"); var destination = await AccountAsync(client, "Destino");
        await client.PostAsJsonAsync("/transactions", new { AccountId = source, Description = "Receita", Amount = "100.02", Type = "Income", OccurredOn = "2024-03-05" });
        await client.PostAsJsonAsync("/transactions", new { AccountId = destination, Description = "Despesa", Amount = "5.01", Type = "Expense", OccurredOn = "2024-03-05" });
        var command = Command(source, destination);
        var response = await client.PostAsJsonAsync("/transfers", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var balances = (await client.GetFromJsonAsync<AccountBalanceDto[]>("/accounts/balances"))!;
        Assert.Equal(90.01m, balances.Single(item => item.Id == source).Balance);
        Assert.Equal(5m, balances.Single(item => item.Id == destination).Balance);
        var summary = (await client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2024-03-31"))!;
        Assert.Equal(95.01m, summary.CurrentBalance);
        Assert.Equal(100.02m, summary.IncomeThisMonth);
        Assert.Equal(5.01m, summary.ExpensesThisMonth);
        Assert.Equal(summary.CurrentBalance, balances.Sum(item => item.Balance));
        Assert.Single((await client.GetFromJsonAsync<TransferDto[]>("/transfers"))!);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/accounts/{source}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/accounts/{destination}")).StatusCode);
    }
    [Fact]
    public async Task ConcurrentRetriesAndTransfersNeverDuplicateOrLoseCents()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var source = await AccountAsync(client, "Origem"); var destination = await AccountAsync(client, "Destino");
        var command = Command(source, destination, .01m);
        var retries = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsJsonAsync("/transfers", command)));
        Assert.Single(retries, item => item.StatusCode == HttpStatusCode.Created);
        Assert.Equal(7, retries.Count(item => item.StatusCode == HttpStatusCode.OK));
        foreach (var response in retries) response.Dispose();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/transfers", command with { Amount = 2m })).StatusCode);
        var distinct = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.PostAsJsonAsync("/transfers", Command(source, destination, .01m))));
        Assert.All(distinct, item => Assert.Equal(HttpStatusCode.Created, item.StatusCode));
        foreach (var response in distinct) response.Dispose();
        var balances = (await client.GetFromJsonAsync<AccountBalanceDto[]>("/accounts/balances"))!;
        Assert.Equal(-.11m, balances.Single(item => item.Id == source).Balance);
        Assert.Equal(.11m, balances.Single(item => item.Id == destination).Balance);
        Assert.Equal(11, (await client.GetFromJsonAsync<TransferDto[]>("/transfers"))!.Length);
    }
    [Fact]
    public async Task InvalidRequestsAndOtherOwnersCannotMoveMoney()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        using var other = await fixture.CreateAuthenticatedClientAsync();
        using var anonymous = fixture.CreateClient();
        var source = await AccountAsync(owner, "Origem"); var destination = await AccountAsync(owner, "Destino");
        var foreign = await AccountAsync(other, "Privada"); var command = Command(source, destination);
        foreach (var route in new[] { "/transfers", "/accounts/balances" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/transfers", command)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync("/transfers", command)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync("/transfers", command with { DestinationAccountId = foreign })).StatusCode);
        foreach (var invalid in new[] { command with { DestinationAccountId = source }, command with { SourceAccountId = Guid.Empty }, command with { Amount = .001m }, command with { Amount = -1m }, command with { OccurredOn = DateOnly.MaxValue }, command with { IdempotencyKey = Guid.Empty } })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/transfers", invalid)).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<TransferDto[]>("/transfers"))!);
        Assert.All((await owner.GetFromJsonAsync<AccountBalanceDto[]>("/accounts/balances"))!, item => Assert.Equal(0m, item.Balance));
        // One user's key does not reserve another user's namespace.
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync("/transfers", command)).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<TransferDto[]>("/transfers"))!);
        var otherDestination = await AccountAsync(other, "Outra própria");
        Assert.Equal(HttpStatusCode.Created, (await other.PostAsJsonAsync("/transfers", command with { SourceAccountId = foreign, DestinationAccountId = otherDestination })).StatusCode);
        // Database composite FKs reject a cross-owner ledger row even outside the API.
        using var scope = fixture.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var userId = (await database.Accounts.IgnoreQueryFilters().SingleAsync(item => item.Id == source)).UserId;
        var failure = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO transfers (id, user_id, source_account_id, destination_account_id, description, amount, occurred_on, idempotency_key) VALUES ({Guid.NewGuid()}, {userId}, {source}, {foreign}, {"Inválida"}, {1m}, {new DateOnly(2024, 3, 5)}, {Guid.NewGuid()})"));
        Assert.Equal(Npgsql.PostgresErrorCodes.ForeignKeyViolation, failure.SqlState);
    }
    [Fact]
    public async Task DatabaseFailureRollsBackTransferAndCacheRevision()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var source = await AccountAsync(client, "Origem"); var destination = await AccountAsync(client, "Destino");
        using var scope = fixture.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var owner = await database.Accounts.IgnoreQueryFilters().Where(item => item.Id == source).Select(item => item.UserId).SingleAsync();
        var before = await database.DashboardCacheRevisions.IgnoreQueryFilters().Where(item => item.UserId == owner).Select(item => item.Revision).SingleAsync();
        // Inject failure after the ledger insert, inside the same SaveChanges transaction.
        await database.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION reject_transfer_revision() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'Injected rollback'; END; $$;
            CREATE TRIGGER reject_transfer_revision BEFORE UPDATE ON dashboard_cache_revisions
            FOR EACH ROW EXECUTE FUNCTION reject_transfer_revision();
            """);
        try
        {
            var failure = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => client.PostAsJsonAsync("/transfers", Command(source, destination)));
            Assert.Equal("P0001", failure.SqlState); // TestServer propagates unhandled DB errors to the client.
            Assert.Equal("Injected rollback", failure.MessageText);
        }
        finally
        {
            await database.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_transfer_revision ON dashboard_cache_revisions; DROP FUNCTION reject_transfer_revision();");
        }
        Assert.Empty((await client.GetFromJsonAsync<TransferDto[]>("/transfers"))!);
        Assert.Equal(before, await database.DashboardCacheRevisions.IgnoreQueryFilters().Where(item => item.UserId == owner).Select(item => item.Revision).SingleAsync());
        Assert.All((await client.GetFromJsonAsync<AccountBalanceDto[]>("/accounts/balances"))!, item => Assert.Equal(0m, item.Balance));
    }
}
