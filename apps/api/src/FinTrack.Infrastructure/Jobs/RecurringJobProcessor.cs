using FinTrack.Infrastructure.Persistence;
using FinTrack.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FinTrack.Infrastructure.Jobs;

public sealed record RecurringJobResult(int UsersProcessed, int GeneratedCount, int FailedUsers, bool HasMore);
public interface IRecurringJobProcessor
{
    Task<RecurringJobResult> RunAsync(CancellationToken cancellationToken);
}

public sealed class RecurringJobProcessor(IServiceScopeFactory scopes, TimeProvider clock,
    BackgroundJobSettings settings, ILogger<RecurringJobProcessor> logger) : IRecurringJobProcessor
{
    public async Task<RecurringJobResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        List<Guid> owners;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            // Trusted job discovery is the only cross-user read. Writes below always select one owner.
            owners = await database.RecurringTransactions.IgnoreQueryFilters()
                .Where(item => item.IsActive && item.NextOccurrenceDate <= today)
                .GroupBy(item => item.UserId).Select(group => new { UserId = group.Key, Next = group.Min(item => item.NextOccurrenceDate) })
                .OrderBy(item => item.Next).ThenBy(item => item.UserId).Take(settings.MaxUsersPerCycle)
                .Select(item => item.UserId).ToListAsync(cancellationToken);
        }
        var processed = 0; var generated = 0; var failed = 0;
        foreach (var owner in owners)
        {
            for (var attempt = 1; attempt <= settings.MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // A fresh DbContext is required for each retry after a transaction failure.
                    await using var scope = scopes.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<RecurringTransactionRepository>();
                    var result = await repository.GenerateForOwnerAsync(owner, today, cancellationToken);
                    processed++; generated += result.GeneratedCount;
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    var transient = exception is TimeoutException or NpgsqlException { IsTransient: true };
                    if (transient && attempt < settings.MaxAttempts)
                    {
                        logger.LogWarning("Recurring job retry {Attempt}: {ErrorType}.", attempt, exception.GetType().Name);
                        await Task.Delay(TimeSpan.FromMilliseconds(settings.RetryDelayMilliseconds * Math.Pow(2, attempt - 1)), clock, cancellationToken);
                        continue;
                    }
                    failed++;
                    logger.LogError("Recurring job owner failed after {Attempts} attempts: {ErrorType}.", attempt, exception.GetType().Name);
                    break;
                }
            }
        }
        bool hasMore;
        await using (var scope = scopes.CreateAsyncScope())
        {
            hasMore = await scope.ServiceProvider.GetRequiredService<FinTrackDbContext>()
                .RecurringTransactions.IgnoreQueryFilters().AnyAsync(item => item.IsActive && item.NextOccurrenceDate <= today, cancellationToken);
        }
        return new(processed, generated, failed, hasMore);
    }
}
