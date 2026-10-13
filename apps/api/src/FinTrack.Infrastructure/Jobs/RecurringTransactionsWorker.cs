using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinTrack.Infrastructure.Jobs;

public sealed class RecurringTransactionsWorker(IRecurringJobProcessor processor, BackgroundJobSettings settings,
    TimeProvider clock, ILogger<RecurringTransactionsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await processor.RunAsync(stoppingToken);
                    logger.LogInformation("Recurring cycle: {Users} users, {Generated} transactions, {Failed} failures, pending={HasMore}.",
                        result.UsersProcessed, result.GeneratedCount, result.FailedUsers, result.HasMore);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    // A failed discovery/cycle must not terminate the API host.
                    logger.LogError("Recurring cycle failed: {ErrorType}. Next cycle will retry.", exception.GetType().Name);
                }
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), clock, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
