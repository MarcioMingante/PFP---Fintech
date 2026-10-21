using System.Diagnostics;
using System.Collections.Concurrent;
using FinTrack.Infrastructure.Observability;
using FinTrack.Application.Recurring;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FinTrack.Application.Transactions;
using FinTrack.Domain.Transactions;
using FinTrack.Infrastructure.Messaging;
using FinTrack.Infrastructure.Persistence;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace FinTrack.IntegrationTests;

public sealed class MessagingTests : IAsyncLifetime
{
    private readonly PostgresApiFixture fixture = new();
    private IContainer? broker;
    private IConnection? connection;
    private IChannel? channel;
    private MessagingWorker Worker => new(fixture.Services.GetRequiredService<IServiceScopeFactory>(),
        new(false, null), NullLogger<MessagingWorker>.Instance);

    public async Task InitializeAsync()
    {
        try
        {
            await fixture.InitializeAsync();
            broker = new ContainerBuilder("rabbitmq:4-alpine")
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "test")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "test-password")
                .WithPortBinding(5672, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete"))
                .Build();
            using var startup = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await broker.StartAsync(startup.Token);
            connection = await new ConnectionFactory { HostName = broker.Hostname,
                Port = broker.GetMappedPublicPort(5672), UserName = "test", Password = "test-password" }.CreateConnectionAsync(startup.Token);
            channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), startup.Token);
            await MessagingWorker.DeclareAsync(channel, startup.Token);
        }
        catch { await DisposeAsync(); throw; }
    }
    public async Task DisposeAsync()
    {
        try
        {
            if (channel is not null) await channel.DisposeAsync();
            if (connection is not null) await connection.DisposeAsync();
        }
        finally
        {
            try { if (broker is not null) await broker.DisposeAsync(); }
            finally { await fixture.DisposeAsync(); }
        }
    }
    private sealed record Resource(Guid Id);
    private async Task<TransactionDto> CreateTransaction(string? traceParent = null)
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var accountResponse = await client.PostAsJsonAsync("/accounts", new { Name = "Messaging" });
        accountResponse.EnsureSuccessStatusCode();
        var account = (await accountResponse.Content.ReadFromJsonAsync<Resource>())!.Id;
        if (traceParent is not null) client.DefaultRequestHeaders.Add("traceparent", traceParent);
        var response = await client.PostAsJsonAsync("/transactions", new CreateTransactionCommand(account, null, "Teste", 12.34m, "Expense", new(2026, 1, 1)));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
    }
    private async Task<int> Count(string table)
    {
        var sql = table switch
        {
            "messaging_outbox" => "SELECT count(*)::integer AS \"Value\" FROM messaging_outbox",
            "transaction_analytics" => "SELECT count(*)::integer AS \"Value\" FROM transaction_analytics",
            "transaction_notifications" => "SELECT count(*)::integer AS \"Value\" FROM transaction_notifications",
            _ => throw new ArgumentException("Tabela inválida.")
        };
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<FinTrackDbContext>().Database
            .SqlQueryRaw<int>(sql).SingleAsync();
    }
    [Fact]
    public async Task RequestOutboxProducerAndConsumers_ShareTraceAcrossDelayedPublication()
    {
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is FinTrackTelemetry.Name or "Npgsql" or "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue
        };
        ActivitySource.AddActivityListener(listener);
        var traceId = ActivityTraceId.CreateRandom();
        var parentSpan = ActivitySpanId.CreateRandom();
        await CreateTransaction($"00-{traceId}-{parentSpan}-01");
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var parent = await scope.ServiceProvider.GetRequiredService<FinTrackDbContext>().Database
                .SqlQueryRaw<string>("SELECT trace_parent AS \"Value\" FROM messaging_outbox").SingleAsync();
            Assert.Equal(traceId, FinTrackTelemetry.Extract(parent, null).TraceId);
        }
        await Worker.PublishBatchAsync(channel!, default);
        foreach (var queue in MessagingWorker.Queues) await Worker.ConsumeOneAsync(channel!, queue, default);
        var spans = stopped.Where(activity => activity.TraceId == traceId).ToArray();
        var server = Assert.Single(spans, activity => activity.Kind == ActivityKind.Server);
        Assert.Equal(parentSpan, server.ParentSpanId);
        Assert.Contains(spans, activity => activity.Source.Name == "Npgsql" && activity.ParentSpanId == server.SpanId);
        var producer = Assert.Single(spans, activity => activity.Kind == ActivityKind.Producer && activity.Source.Name == FinTrackTelemetry.Name);
        Assert.Equal(server.SpanId, producer.ParentSpanId);
        var consumers = spans.Where(activity => activity.Kind == ActivityKind.Consumer && activity.Source.Name == FinTrackTelemetry.Name).ToArray();
        Assert.Equal(2, consumers.Length);
        Assert.All(consumers, activity => Assert.Equal(producer.SpanId, activity.ParentSpanId));
        Assert.All(consumers, activity => Assert.Contains(spans, child => child.Source.Name == "Npgsql" && child.ParentSpanId == activity.SpanId));
    }
    [Fact]
    public async Task CommittedManualTransaction_FanoutAndDuplicateDelivery_ProduceOneProjectionPerConsumer()
    {
        var item = await CreateTransaction();
        Assert.Equal(1, await Count("messaging_outbox"));
        Assert.Equal(0, await Count("transaction_analytics"));
        await Worker.PublishBatchAsync(channel!, default);
        foreach (var queue in MessagingWorker.Queues) await Worker.ConsumeOneAsync(channel!, queue, default);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            await database.Database.ExecuteSqlRawAsync("UPDATE messaging_outbox SET published_at = NULL");
        }
        await Worker.PublishBatchAsync(channel!, default);
        foreach (var queue in MessagingWorker.Queues) await Worker.ConsumeOneAsync(channel!, queue, default);
        Assert.Equal(1, await Count("transaction_analytics"));
        Assert.Equal(1, await Count("transaction_notifications"));
        await using var check = fixture.Services.CreateAsyncScope();
        var amount = await check.ServiceProvider.GetRequiredService<FinTrackDbContext>().Database
            .SqlQueryRaw<decimal>("SELECT amount AS \"Value\" FROM transaction_analytics").SingleAsync();
        Assert.Equal(item.Amount, amount);
    }
    [Fact]
    public async Task FailedPublish_PreservesPendingOutbox_AndRecoveryDelivers()
    {
        await CreateTransaction();
        await using var closed = await connection!.CreateChannelAsync(new CreateChannelOptions(true, true));
        await closed.CloseAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => Worker.PublishBatchAsync(closed, default));
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var published = await scope.ServiceProvider.GetRequiredService<FinTrackDbContext>().Database
                .SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM messaging_outbox WHERE published_at IS NOT NULL").SingleAsync();
            Assert.Equal(0, published);
        }
        await Worker.PublishBatchAsync(channel!, default);
        foreach (var queue in MessagingWorker.Queues) await Worker.ConsumeOneAsync(channel!, queue, default);
        Assert.Equal(1, await Count("transaction_notifications"));
    }
    [Fact]
    public async Task InvalidEvent_GoesToEachDeadLetterQueue_WithoutProjection()
    {
        await channel!.BasicPublishAsync(MessagingWorker.Exchange, "", true, new BasicProperties { Persistent = true }, Encoding.UTF8.GetBytes("invalid-json"));
        foreach (var queue in MessagingWorker.Queues)
        {
            await Worker.ConsumeOneAsync(channel, queue, default);
            BasicGetResult? dead = null;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (dead is null)
            {
                dead = await channel.BasicGetAsync(queue + ".dead", true, timeout.Token);
                if (dead is null) await Task.Delay(50, timeout.Token);
            }
            Assert.Equal("invalid-json", Encoding.UTF8.GetString(dead.Body.Span));
        }
        Assert.Equal(0, await Count("transaction_analytics"));
        Assert.Equal(0, await Count("transaction_notifications"));
    }
    [Fact]
    public async Task RecurringGeneration_ConcurrentPublishers_EmitOneEventPerOccurrence()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/accounts", new { Name = "Recorrência" });
        var account = (await response.Content.ReadFromJsonAsync<Resource>())!.Id;
        (await client.PostAsJsonAsync("/recurring-transactions", new CreateRecurringTransactionCommand(account, null,
            "Mensal", 3m, "Expense", new(2024, 1, 31), new(2024, 3, 31)))).EnsureSuccessStatusCode();
        for (var i = 0; i < 2; i++)
            (await client.PostAsJsonAsync("/recurring-transactions/generate", new GenerateRecurringTransactionsRequest(new(2024, 3, 31)))).EnsureSuccessStatusCode();
        Assert.Equal(3, await Count("messaging_outbox"));
        await using var otherChannel = await connection!.CreateChannelAsync(new CreateChannelOptions(true, true));
        await Task.WhenAll(Worker.PublishBatchAsync(channel!, default), Worker.PublishBatchAsync(otherChannel, default));
        foreach (var queue in MessagingWorker.Queues)
        {
            Assert.Equal(3u, (await channel!.QueueDeclarePassiveAsync(queue)).MessageCount);
            for (var i = 0; i < 3; i++) await Worker.ConsumeOneAsync(channel, queue, default);
        }
        Assert.Equal(3, await Count("transaction_analytics"));
        Assert.Equal(3, await Count("transaction_notifications"));
    }
    [Fact]
    public async Task SynchronousSave_PersistsOutboxWithTransaction()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/accounts", new { Name = "Sync" });
        var accountId = (await response.Content.ReadFromJsonAsync<Resource>())!.Id;
        await using var scope = fixture.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var account = await database.Accounts.IgnoreQueryFilters().SingleAsync(a => a.Id == accountId);
        database.Transactions.Add(new Transaction(account.UserId!.Value, accountId, null, null, "Sync", 7,
            TransactionType.Income, new(2026, 1, 1)));
        database.SaveChanges();
        Assert.Equal(1, await Count("messaging_outbox"));
        await Worker.PublishBatchAsync(channel!, default);
        foreach (var queue in MessagingWorker.Queues) await Worker.ConsumeOneAsync(channel!, queue, default);
        Assert.Equal(1, await Count("transaction_analytics"));
    }
    [Fact]
    public async Task BackgroundWorker_DeliversAndStopsCleanly()
    {
        await CreateTransaction();
        var uri = $"amqp://test:test-password@{broker!.Hostname}:{broker.GetMappedPublicPort(5672)}/";
        using var worker = new MessagingWorker(fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            new(true, uri), NullLogger<MessagingWorker>.Instance);
        await worker.StartAsync(default);
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (await Count("transaction_notifications") == 0) await Task.Delay(100, deadline.Token);
            Assert.Equal(1, await Count("transaction_analytics"));
        }
        finally
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StopAsync(stop.Token);
        }
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }
    [Fact]
    public async Task ConsumerDatabaseFailure_ExhaustsRetriesAndMovesEventToDeadLetterQueue()
    {
        await CreateTransaction();
        await Worker.PublishBatchAsync(channel!, default);
        await using (var scope = fixture.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<FinTrackDbContext>().Database
                .ExecuteSqlRawAsync("ALTER TABLE transaction_analytics RENAME TO unavailable_analytics");
        try
        {
            await Worker.ConsumeOneAsync(channel!, MessagingWorker.Queues[0], default);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            BasicGetResult? dead = null;
            while (dead is null)
            {
                dead = await channel!.BasicGetAsync(MessagingWorker.Queues[0] + ".dead", true, deadline.Token);
                if (dead is null) await Task.Delay(50, deadline.Token);
            }
            Assert.NotNull(JsonSerializer.Deserialize<TransactionCreated>(dead.Body.Span));
            await Worker.ConsumeOneAsync(channel!, MessagingWorker.Queues[1], default);
            Assert.Equal(1, await Count("transaction_notifications"));
        }
        finally
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<FinTrackDbContext>().Database
                .ExecuteSqlRawAsync("ALTER TABLE unavailable_analytics RENAME TO transaction_analytics");
        }
        Assert.Equal(0, await Count("transaction_analytics"));
    }
    [Fact]
    public async Task RolledBackTransaction_LeavesNoOutboxOrConsumerEffects()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var accountResponse = await client.PostAsJsonAsync("/accounts", new { Name = "Rollback" });
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<Resource>())!.Id;
        await using var scope = fixture.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var account = await database.Accounts.IgnoreQueryFilters().SingleAsync(a => a.Id == accountId);
        await using (var transaction = await database.Database.BeginTransactionAsync())
        {
            database.Transactions.Add(new Transaction(account.UserId!.Value, accountId, null, null, "Rollback", 5, TransactionType.Income, new(2026, 1, 1)));
            await database.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.Equal(0, await Count("messaging_outbox"));
        await Worker.PublishBatchAsync(channel!, default);
        Assert.Equal(0, await Count("transaction_analytics"));
    }
}
