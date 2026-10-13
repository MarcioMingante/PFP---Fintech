using System.Text;
using System.Text.Json;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FinTrack.Infrastructure.Messaging;

public sealed class MessagingWorker(IServiceScopeFactory scopes, MessagingSettings settings,
    ILogger<MessagingWorker> logger) : BackgroundService
{
    public const string Exchange = "fintrack.transaction-created.v1";
    public static readonly string[] Queues = ["fintrack.analytics.v1", "fintrack.notifications.v1"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory { Uri = new Uri(settings.ConnectionString!), AutomaticRecoveryEnabled = false };
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), stoppingToken);
                await DeclareAsync(channel, stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await PublishBatchAsync(channel, stoppingToken);
                    for (var i = 0; i < 50; i++)
                        foreach (var queue in Queues) await ConsumeOneAsync(channel, queue, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                // Never include broker URI, financial payload or exception message in logs.
                logger.LogWarning("Messaging connection/cycle failed ({ErrorType}); retrying", exception.GetType().Name);
                try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }

    public static async Task DeclareAsync(IChannel channel, CancellationToken token)
    {
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Fanout, durable: true, cancellationToken: token);
        foreach (var queue in Queues)
        {
            await channel.QueueDeclareAsync(queue + ".dead", durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" }, cancellationToken: token);
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum", ["x-dead-letter-strategy"] = "at-least-once", ["x-overflow"] = "reject-publish", ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = queue + ".dead" }, cancellationToken: token);
            await channel.QueueBindAsync(queue, Exchange, "", cancellationToken: token);
        }
    }

    public async Task PublishBatchAsync(IChannel channel, CancellationToken token)
    {
        await using var scope = scopes.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        // Lock pending rows until confirmations and commit. Concurrent API instances skip locked work.
        var rows = await database.Database.SqlQueryRaw<PendingEvent>("""
            SELECT event_id AS "EventId", payload::text AS "Payload" FROM messaging_outbox
            WHERE published_at IS NULL ORDER BY created_at, event_id LIMIT 50 FOR UPDATE SKIP LOCKED
            """).ToListAsync(token);
        foreach (var row in rows)
        {
            var properties = new BasicProperties { Persistent = true, ContentType = "application/json",
                Type = "TransactionCreated", MessageId = row.EventId.ToString() };
            await channel.BasicPublishAsync(Exchange, "", mandatory: true, properties, Encoding.UTF8.GetBytes(row.Payload), token);
            await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE messaging_outbox SET published_at = now() WHERE event_id = {row.EventId}", token);
        }
        await transaction.CommitAsync(token);
    }

    public async Task ConsumeOneAsync(IChannel channel, string queue, CancellationToken token)
    {
        if (!Queues.Contains(queue)) throw new ArgumentException("Fila inválida.", nameof(queue));
        var delivery = await channel.BasicGetAsync(queue, autoAck: false, cancellationToken: token);
        if (delivery is null) return;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var item = JsonSerializer.Deserialize<TransactionCreated>(delivery.Body.Span)
                    ?? throw new JsonException("Evento ausente.");
                if (item.Version != 1 || item.EventId == Guid.Empty || item.EventId != item.TransactionId || item.UserId == Guid.Empty
                    || item.Amount <= 0 || item.Amount > 9999999999999999.99m || decimal.Round(item.Amount, 2) != item.Amount
                    || !Enum.IsDefined(item.Type)) throw new JsonException("Evento inválido.");
                await using var scope = scopes.CreateAsyncScope();
                var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
                // The projection row is also the inbox marker: one atomic idempotent statement.
                if (queue == Queues[0])
                    await database.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO transaction_analytics (event_id, transaction_id, user_id, amount, type, occurred_on)
                        VALUES ({item.EventId}, {item.TransactionId}, {item.UserId}, {item.Amount}, {(int)item.Type}, {item.OccurredOn})
                        ON CONFLICT (event_id) DO NOTHING
                        """, token);
                else
                    await database.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO transaction_notifications (event_id, transaction_id, user_id)
                        VALUES ({item.EventId}, {item.TransactionId}, {item.UserId}) ON CONFLICT (event_id) DO NOTHING
                        """, token);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: token);
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogWarning("Consumer {Consumer} attempt {Attempt} failed ({ErrorType})", queue, attempt, exception.GetType().Name);
                if (attempt == 3 || exception is JsonException)
                {
                    await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: token);
                    return;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), token);
            }
        }
    }

    public sealed class PendingEvent
    {
        public Guid EventId { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}
