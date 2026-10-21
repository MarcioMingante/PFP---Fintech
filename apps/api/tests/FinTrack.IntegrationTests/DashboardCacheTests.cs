using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Dashboard;
using FinTrack.Application.Transactions;
using FinTrack.Application.Purchases;
using FinTrack.Application.Recurring;
using FinTrack.Infrastructure.Caching;
using FinTrack.Infrastructure.Persistence;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FinTrack.IntegrationTests;

public sealed class DashboardCacheTests
{
    private sealed class AggregationCounter : DbCommandInterceptor
    {
        private int count;
        public int Count => Volatile.Read(ref count);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("GROUP BY", StringComparison.Ordinal)) Interlocked.Increment(ref count);
            return ValueTask.FromResult(result);
        }
    }
    private sealed class SnapshotBlocker : DbCommandInterceptor
    {
        private int queries;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("GROUP BY", StringComparison.Ordinal) && Interlocked.Increment(ref queries) == 2)
            {
                Entered.TrySetResult();
                await Resume.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }
    private sealed record Resource(Guid Id, string Name);
    private sealed record Identity(Guid Id);
    private static async Task<Guid> Create(HttpClient client, string route, object payload)
    {
        var response = await client.PostAsJsonAsync(route, payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Resource>())!.Id;
    }
    private static async Task<DashboardSummaryDto> Summary(HttpClient client, string date = "2024-03-31")
        => (await client.GetFromJsonAsync<DashboardSummaryDto>($"/dashboard/summary?asOf={date}"))!;
    private static async Task<long> Revision(PostgresApiFixture fixture, Guid userId)
    {
        using var scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<FinTrackDbContext>().DashboardCacheRevisions.IgnoreQueryFilters()
            .Where(item => item.UserId == userId).Select(item => item.Revision).SingleOrDefaultAsync();
    }

    [Fact]
    public async Task RedisHit_SkipsAggregationsAndMutationsInvalidateAllUserDates()
    {
        await using var redis = new RedisBuilder("redis:7-alpine").Build();
        await redis.StartAsync();
        var counter = new AggregationCounter();
        var fixture = new PostgresApiFixture { RedisConnectionString = redis.GetConnectionString(),
            ConfigureServices = services => services.AddDbContext<FinTrackDbContext>(options => options.AddInterceptors(counter)) };
        try
        {
            await fixture.InitializeAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var userId = (await client.GetFromJsonAsync<Identity>("/auth/me"))!.Id;
            await Summary(client);
            var firstCount = counter.Count;
            Assert.True(firstCount > 0);
            await Summary(client);
            Assert.Equal(firstCount, counter.Count);
            await Summary(client, "2024-02-29");
            Assert.True(counter.Count > firstCount);
            var account = await Create(client, "/accounts", new { Name = "Conta cache" });
            var category = await Create(client, "/categories", new { Name = "Categoria cache" });
            var before = await Revision(fixture, userId);
            var response = await client.PostAsJsonAsync("/transactions", new CreateTransactionCommand(account, category, "Despesa", 10.01m, "Expense", new(2024, 3, 1)));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.True(await Revision(fixture, userId) > before);
            Assert.Equal(-10.01m, (await Summary(client)).CurrentBalance);
            using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
            var key = RedisDashboardCache.Key(userId, new(2024, 3, 31), await Revision(fixture, userId));
            var ttl = await connection.GetDatabase().KeyTimeToLiveAsync(key);
            Assert.NotNull(ttl); Assert.InRange(ttl.Value.TotalSeconds, 1, 60);
            var countAfterFill = counter.Count;
            await Summary(client); Assert.Equal(countAfterFill, counter.Count);
            Assert.Equal(0m, (await Summary(client, "2024-02-29")).CurrentBalance);
            using var other = await fixture.CreateAuthenticatedClientAsync();
            Assert.Equal(0m, (await Summary(other)).CurrentBalance);
            Assert.Equal(0m, (await Summary(other)).ExpensesThisMonth);
            var updated = await client.PutAsJsonAsync($"/categories/{category}", new { Name = "Nome atualizado" });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal("Nome atualizado", Assert.Single((await Summary(client)).ExpensesByCategory).Name);
            before = await Revision(fixture, userId);
            var invalid = await client.PostAsJsonAsync("/transactions", new CreateTransactionCommand(account, null, "Inválida", 0m, "Expense", new(2024, 3, 1)));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal(before, await Revision(fixture, userId));
            // Malformed Redis entries do not break financial reads.
            key = RedisDashboardCache.Key(userId, new(2024, 3, 31), before);
            await connection.GetDatabase().StringSetAsync(key, "{invalid-json", TimeSpan.FromSeconds(60));
            Assert.Equal(-10.01m, (await Summary(client)).CurrentBalance);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task TtlExpiry_RecomputesAndRedisOutageDoesNotBlockReadsOrWrites()
    {
        await using var redis = new RedisBuilder("redis:7-alpine").Build();
        await redis.StartAsync();
        var counter = new AggregationCounter();
        var fixture = new PostgresApiFixture { RedisConnectionString = redis.GetConnectionString(), CacheTtlSeconds = 1,
            ConfigureServices = services => services.AddDbContext<FinTrackDbContext>(options => options.AddInterceptors(counter)) };
        try
        {
            await fixture.InitializeAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var account = await Create(client, "/accounts", new { Name = "Conta fallback" });
            var userId = (await client.GetFromJsonAsync<Identity>("/auth/me"))!.Id;
            await Summary(client);
            var filledCount = counter.Count;
            using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
            var key = RedisDashboardCache.Key(userId, new(2024, 3, 31), await Revision(fixture, userId));
            for (var attempt = 0; attempt < 40 && await connection.GetDatabase().KeyExistsAsync(key); attempt++)
                await Task.Delay(100);
            Assert.False(await connection.GetDatabase().KeyExistsAsync(key));
            await Summary(client); Assert.True(counter.Count > filledCount);
            await redis.StopAsync();
            var response = await client.PostAsJsonAsync("/transactions", new CreateTransactionCommand(account, null, "Durante outage", 100m, "Income", new(2024, 3, 1)));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(100m, (await Summary(client)).CurrentBalance);
            Assert.Equal(100m, (await Summary(client)).CurrentBalance);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task PurchasesRecurringChangesAndGeneration_UpdateCachedForecast()
    {
        await using var redis = new RedisBuilder("redis:7-alpine").Build();
        await redis.StartAsync();
        var fixture = new PostgresApiFixture { RedisConnectionString = redis.GetConnectionString() };
        try
        {
            await fixture.InitializeAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var account = await Create(client, "/accounts", new { Name = "Conta" });
            await Summary(client);
            var created = await client.PostAsJsonAsync("/recurring-transactions", new CreateRecurringTransactionCommand(account, null, "Mensal", 10m, "Expense", new(2024, 3, 1), null));
            var recurring = (await created.Content.ReadFromJsonAsync<RecurringTransactionDto>())!;
            Assert.Equal(30m, (await Summary(client)).FutureExpenses);
            var generated = await client.PostAsJsonAsync("/recurring-transactions/generate", new GenerateRecurringTransactionsRequest(new(2024, 3, 31)));
            Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
            Assert.Equal(-10m, (await Summary(client)).CurrentBalance);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/recurring-transactions/{recurring.Id}/deactivate", null)).StatusCode);
            Assert.Equal(0m, (await Summary(client)).FutureExpenses);
            var card = await Create(client, "/credit-cards", new { Name = "Cartão", Limit = 1000m, ClosingDay = 25, DueDay = 5 });
            var purchase = await client.PostAsJsonAsync($"/credit-cards/{card}/purchases", new CreatePurchaseCommand("Compra", 100m, 3, new(2024, 1, 25), Guid.NewGuid()));
            Assert.Equal(HttpStatusCode.Created, purchase.StatusCode);
            var summary = await Summary(client);
            Assert.Equal(33.34m, summary.CreditCardInvoicesThisMonth);
            Assert.Equal(66.66m, summary.FutureExpenses);
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/credit-cards/{card}", new { Name = "Cartão renomeado", Limit = 1000m, ClosingDay = 25, DueDay = 5 })).StatusCode);
            Assert.Equal("Cartão renomeado", Assert.Single((await Summary(client)).Invoices).CardName);
        }
        finally { await fixture.DisposeAsync(); }
    }
    [Fact]
    public async Task RedisTimeout_RecomputesInsteadOfFailingFinancialRead()
    {
        await using var redis = new RedisBuilder("redis:7-alpine").Build();
        await redis.StartAsync();
        var counter = new AggregationCounter();
        var fixture = new PostgresApiFixture { RedisConnectionString = redis.GetConnectionString(),
            ConfigureServices = services => services.AddDbContext<FinTrackDbContext>(options => options.AddInterceptors(counter)) };
        try
        {
            await fixture.InitializeAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync();
            await Summary(client);
            var filled = counter.Count;
            using var connection = await ConnectionMultiplexer.ConnectAsync($"{redis.GetConnectionString()},allowAdmin=true");
            await connection.GetDatabase().ExecuteAsync("CLIENT", "PAUSE", "3000", "ALL");
            Assert.Equal(0m, (await Summary(client)).CurrentBalance);
            Assert.True(counter.Count > filled);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task ConcurrentCommit_CannotPublishOldSnapshotUnderNewRevision()
    {
        await using var redis = new RedisBuilder("redis:7-alpine").Build();
        await redis.StartAsync();
        var blocker = new SnapshotBlocker();
        var fixture = new PostgresApiFixture { RedisConnectionString = redis.GetConnectionString(),
            ConfigureServices = services => services.AddDbContext<FinTrackDbContext>(options => options.AddInterceptors(blocker)) };
        try
        {
            await fixture.InitializeAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var account = await Create(client, "/accounts", new { Name = "Conta concorrente" });
            var read = Summary(client);
            await blocker.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var write = await client.PostAsJsonAsync("/transactions", new CreateTransactionCommand(account, null,
                "Após snapshot", 100m, "Income", new(2024, 3, 1)));
            Assert.Equal(HttpStatusCode.Created, write.StatusCode);
            blocker.Resume.TrySetResult();
            Assert.Equal(0m, (await read).CurrentBalance);
            Assert.Equal(100m, (await Summary(client)).CurrentBalance);
            Assert.Equal(100m, (await Summary(client)).CurrentBalance);
        }
        finally { blocker.Resume.TrySetResult(); await fixture.DisposeAsync(); }
    }

}
