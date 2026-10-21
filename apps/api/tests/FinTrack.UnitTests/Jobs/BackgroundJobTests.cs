using FinTrack.Infrastructure.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinTrack.UnitTests.Jobs;

public sealed class BackgroundJobTests
{
    [Fact]
    public void Defaults_AreBoundedAndDoNotStartWorkerImplicitly()
    {
        var settings = BackgroundJobSettings.Load(new ConfigurationBuilder().Build());
        Assert.False(settings.Enabled);
        Assert.Equal((60, 3, 500, 100), (settings.IntervalSeconds, settings.MaxAttempts, settings.RetryDelayMilliseconds, settings.MaxUsersPerCycle));
    }

    [Theory]
    [InlineData("IntervalSeconds", "0")]
    [InlineData("MaxAttempts", "6")]
    [InlineData("RetryDelayMilliseconds", "49")]
    [InlineData("MaxUsersPerCycle", "1001")]
    public void InvalidConfiguration_FailsBeforeStartingWorker(string property, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [$"BackgroundJobs:{property}"] = value }).Build();
        Assert.Throws<InvalidOperationException>(() => BackgroundJobSettings.Load(configuration));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Registration_OnlySchedulesEnabledWorker(bool enabled)
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["BackgroundJobs:Enabled"] = enabled.ToString() }).Build());
        Assert.Equal(enabled, services.Any(item => item.ServiceType == typeof(IHostedService) && item.ImplementationType == typeof(RecurringTransactionsWorker)));
    }

    private sealed class Processor(Func<CancellationToken, Task<RecurringJobResult>> execute) : IRecurringJobProcessor
    {
        public Task<RecurringJobResult> RunAsync(CancellationToken cancellationToken) => execute(cancellationToken);
    }

    [Fact]
    public async Task Shutdown_CancelsInFlightProcessingAndCompletesService()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processor = new Processor(async token =>
        {
            entered.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
            return new(0, 0, 0, false);
        });
        using var worker = new RecurringTransactionsWorker(processor, new(false, 60, 3, 50, 100), TimeProvider.System, NullLogger<RecurringTransactionsWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StopAsync(deadline.Token);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task CycleFailure_DoesNotTerminateWorkerAndNextCycleRuns()
    {
        var calls = 0;
        var resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processor = new Processor(_ =>
        {
            if (Interlocked.Increment(ref calls) == 1) throw new InvalidOperationException("Simulated discovery failure");
            resumed.TrySetResult();
            return Task.FromResult(new RecurringJobResult(0, 0, 0, false));
        });
        using var worker = new RecurringTransactionsWorker(processor, new(false, 1, 3, 50, 100), TimeProvider.System, NullLogger<RecurringTransactionsWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await resumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StopAsync(deadline.Token);
        Assert.True(calls >= 2);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }
}
