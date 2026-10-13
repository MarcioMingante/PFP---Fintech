using FinTrack.Infrastructure.Observability;
using System.Diagnostics;
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
                using var activity = FinTrackTelemetry.Activities.StartActivity("Recurring generation cycle");
                try
                {
                    var result = await processor.RunAsync(stoppingToken);
                    FinTrackTelemetry.Generated.Add(result.GeneratedCount);
                    FinTrackTelemetry.JobCycles.Add(1, new KeyValuePair<string, object?>("outcome", result.FailedUsers > 0 ? "partial_failure" : "success"));
                    if (result.FailedUsers > 0) activity?.SetStatus(ActivityStatusCode.Error);
                    logger.LogInformation("Recurring cycle: {Users} users, {Generated} transactions, {Failed} failures, pending={HasMore}.",
                        result.UsersProcessed, result.GeneratedCount, result.FailedUsers, result.HasMore);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    FinTrackTelemetry.JobCycles.Add(1, new KeyValuePair<string, object?>("outcome", "failure"));
                    // A failed discovery/cycle must not terminate the API host.
                    logger.LogError("Recurring cycle failed: {ErrorType}. Next cycle will retry.", exception.GetType().Name);
                }
                activity?.Stop(); // Processing duration excludes idle interval.
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), clock, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
