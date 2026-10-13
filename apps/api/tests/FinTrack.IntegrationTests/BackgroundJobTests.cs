using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Dashboard;
using FinTrack.Application.Recurring;
using FinTrack.Infrastructure.Jobs;
using FinTrack.Infrastructure.Persistence;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Redis;

namespace FinTrack.IntegrationTests;

public sealed class BackgroundJobTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
    private static readonly TimeProvider MarchClock = new Clock(new(2024, 3, 31, 12, 0, 0, TimeSpan.Zero));
    private sealed record Account(Guid Id, string Name);
    private sealed class LockFault : DbCommandInterceptor
    {
        public int TransientFailures = 1;
        public string? PermanentOwner;
        public bool FailAfterSave;
        public bool Armed;
        public int Attempts;
        public int PermanentAttempts;
        public ConcurrentBag<Guid> Contexts { get; } = [];
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref Attempts);
                Contexts.Add(eventData.Context!.ContextId.InstanceId);
                if (PermanentOwner is not null && command.Parameters.Cast<DbParameter>().Any(parameter => parameter.Value?.ToString() == PermanentOwner))
                {
                    Interlocked.Increment(ref PermanentAttempts);
                    throw new InvalidOperationException("Simulated permanent owner failure");
                }
                if (!FailAfterSave && Interlocked.Decrement(ref TransientFailures) >= 0) throw new TimeoutException("Simulated transient failure");
            }
            if (FailAfterSave && Armed && command.CommandText.Contains("INSERT INTO dashboard_cache_revisions", StringComparison.Ordinal)
                && Interlocked.Decrement(ref TransientFailures) >= 0)
                throw new TimeoutException("Simulated failure after transactional data write");
            return ValueTask.FromResult(result);
        }
    }
    private static async Task<Guid> AccountId(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/accounts", new { Name = "Job account" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Account>())!.Id;
    }
    private static async Task<RecurringTransactionDto> Rule(HttpClient client, Guid account, DateOnly? start = null, DateOnly? end = null, string type = "Expense")
    {
        var response = await client.PostAsJsonAsync("/recurring-transactions", new CreateRecurringTransactionCommand(account, null, "Monthly job", 10m,
            type, start ?? new(2024, 1, 31), end ?? new(2024, 3, 31)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RecurringTransactionDto>())!;
    }
    private static async Task<TransactionDto[]> Transactions(HttpClient client)
        => (await client.GetFromJsonAsync<TransactionDto[]>("/transactions"))!;

    [Fact]
    public async Task HostedWorker_GeneratesWithoutManualEndpointAndInvalidatesDashboardCache()
    {
        await using var redis = new RedisBuilder("redis:7-alpine").Build();
        await redis.StartAsync();
        var fixture = new PostgresApiFixture { RedisConnectionString = redis.GetConnectionString(), EnableBackgroundJobs = true,
            BackgroundJobIntervalSeconds = 1, ConfigureServices = services => services.AddSingleton(MarchClock) };
        RecurringTransactionsWorker? worker = null;
        try
        {
            await fixture.InitializeAsync();
            worker = Assert.Single(fixture.Services.GetServices<IHostedService>().OfType<RecurringTransactionsWorker>());
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var account = await AccountId(client);
            Assert.Equal(0m, (await client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2024-03-31"))!.CurrentBalance);
            await Rule(client, account);
            TransactionDto[] rows = [];
            for (var attempt = 0; attempt < 100; attempt++)
            {
                rows = await Transactions(client);
                if (rows.Length == 3) break;
                await Task.Delay(50);
            }
            Assert.Equal(3, rows.Length);
            Assert.Equal(new[] { new DateOnly(2024, 1, 31), new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 31) }, rows.OrderBy(item => item.OccurredOn).Select(item => item.OccurredOn));
            var summary = (await client.GetFromJsonAsync<DashboardSummaryDto>("/dashboard/summary?asOf=2024-03-31"))!;
            Assert.Equal(-30m, summary.CurrentBalance);
            Assert.Equal(10m, summary.ExpensesThisMonth);
            Assert.Equal(0m, summary.FutureExpenses);
            await fixture.DisposeAsync();
            Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task ConcurrentJobCyclesAndManualGeneration_DoNotDuplicateOrMixOwners()
    {
        var fixture = new PostgresApiFixture { ConfigureServices = services => services.AddSingleton(MarchClock) };
        try
        {
            await fixture.InitializeAsync();
            using var first = await fixture.CreateAuthenticatedClientAsync();
            using var second = await fixture.CreateAuthenticatedClientAsync();
            var firstAccount = await AccountId(first);
            var firstRule = await Rule(first, firstAccount);
            await Rule(first, firstAccount, new(2024, 4, 1), new(2024, 5, 1)); // Future occurrences stay pending.
            await Rule(second, await AccountId(second), new(2024, 2, 1), new(2024, 3, 1), "Income");
            var processor = fixture.Services.GetRequiredService<IRecurringJobProcessor>();
            var jobs = Enumerable.Range(0, 4).Select(_ => processor.RunAsync(CancellationToken.None)).ToArray();
            var manual = first.PostAsJsonAsync("/recurring-transactions/generate", new GenerateRecurringTransactionsRequest(new(2024, 3, 31)));
            await Task.WhenAll(jobs.Cast<Task>().Append(manual));
            using var manualResponse = await manual;
            Assert.Equal(HttpStatusCode.OK, manualResponse.StatusCode);
            var manualResult = (await manualResponse.Content.ReadFromJsonAsync<RecurringGenerationResult>())!;
            var jobResults = await Task.WhenAll(jobs);
            Assert.Equal(5, jobResults.Sum(job => job.GeneratedCount) + manualResult.GeneratedCount);
            var firstRows = await Transactions(first); var secondRows = await Transactions(second);
            Assert.Equal(3, firstRows.Length); Assert.Equal(2, secondRows.Length);
            Assert.All(firstRows, item => Assert.Equal(firstRule.Id, item.RecurringTransactionId));
            Assert.All(firstRows, item => Assert.Equal(firstAccount, item.AccountId));
            Assert.All(secondRows, item => Assert.Equal("Income", item.Type));
            Assert.Equal(3, firstRows.Select(item => (item.RecurringTransactionId, item.OccurredOn)).Distinct().Count());
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task TransientFailure_RetriesWithFreshDbContextAndNoPartialProgress()
    {
        var fault = new LockFault { FailAfterSave = true };
        var fixture = new PostgresApiFixture { ConfigureServices = services =>
        {
            services.AddSingleton(MarchClock);
            services.AddDbContext<FinTrackDbContext>(options => options.AddInterceptors(fault));
        } };
        try
        {
            await fixture.InitializeAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync();
            await Rule(client, await AccountId(client));
            fault.Armed = true;
            var result = await fixture.Services.GetRequiredService<IRecurringJobProcessor>().RunAsync(CancellationToken.None);
            Assert.Equal((1, 3, 0, false), (result.UsersProcessed, result.GeneratedCount, result.FailedUsers, result.HasMore));
            Assert.Equal(2, fault.Attempts);
            Assert.Equal(2, fault.Contexts.Distinct().Count());
            Assert.Equal(3, (await Transactions(client)).Length);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task PermanentOwnerFailure_DoesNotStopOtherOwnersAndCanRecoverLater()
    {
        var fault = new LockFault { TransientFailures = 0 };
        var fixture = new PostgresApiFixture { ConfigureServices = services =>
        {
            services.AddSingleton(MarchClock);
            services.AddDbContext<FinTrackDbContext>(options => options.AddInterceptors(fault));
        } };
        try
        {
            await fixture.InitializeAsync();
            using var failed = await fixture.CreateAuthenticatedClientAsync();
            using var healthy = await fixture.CreateAuthenticatedClientAsync();
            var failedRule = await Rule(failed, await AccountId(failed));
            await Rule(healthy, await AccountId(healthy));
            using var identity = System.Text.Json.JsonDocument.Parse(await failed.GetStringAsync("/auth/me"));
            fault.PermanentOwner = identity.RootElement.GetProperty("id").GetString();
            var processor = fixture.Services.GetRequiredService<IRecurringJobProcessor>();
            var result = await processor.RunAsync(CancellationToken.None);
            Assert.Equal((1, 3, 1, true), (result.UsersProcessed, result.GeneratedCount, result.FailedUsers, result.HasMore));
            Assert.Equal(1, fault.PermanentAttempts);
            Assert.Empty(await Transactions(failed));
            Assert.Equal(3, (await Transactions(healthy)).Length);
            Assert.Equal(new DateOnly(2024, 1, 31), (await failed.GetFromJsonAsync<RecurringTransactionDto>($"/recurring-transactions/{failedRule.Id}"))!.NextOccurrenceDate);
            fault.PermanentOwner = null;
            var recovered = await processor.RunAsync(CancellationToken.None);
            Assert.Equal((1, 3, 0, false), (recovered.UsersProcessed, recovered.GeneratedCount, recovered.FailedUsers, recovered.HasMore));
            Assert.Equal(3, (await Transactions(healthy)).Length);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task BoundedBacklog_ResumesAndProcessesRemainingOwnersOnNextCycles()
    {
        var fixture = new PostgresApiFixture { ConfigureServices = services =>
        {
            services.AddSingleton<TimeProvider>(new Clock(new(2010, 12, 31, 12, 0, 0, TimeSpan.Zero)));
            services.AddSingleton(new BackgroundJobSettings(false, 60, 3, 50, 1));
        } };
        try
        {
            await fixture.InitializeAsync();
            using var first = await fixture.CreateAuthenticatedClientAsync();
            using var second = await fixture.CreateAuthenticatedClientAsync();
            await Rule(first, await AccountId(first), new(2000, 1, 31), new(2010, 12, 31));
            await Rule(second, await AccountId(second), new(2010, 12, 1), new(2010, 12, 1));
            var processor = fixture.Services.GetRequiredService<IRecurringJobProcessor>();
            var batch = await processor.RunAsync(CancellationToken.None);
            Assert.Equal((1, 120, true), (batch.UsersProcessed, batch.GeneratedCount, batch.HasMore));
            batch = await processor.RunAsync(CancellationToken.None);
            Assert.Equal((1, 12, true), (batch.UsersProcessed, batch.GeneratedCount, batch.HasMore));
            batch = await processor.RunAsync(CancellationToken.None);
            Assert.Equal((1, 1, false), (batch.UsersProcessed, batch.GeneratedCount, batch.HasMore));
            Assert.Equal(132, (await Transactions(first)).Length);
            Assert.Single(await Transactions(second));
        }
        finally { await fixture.DisposeAsync(); }
    }
}
