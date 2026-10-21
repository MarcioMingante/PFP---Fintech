using Microsoft.Extensions.Configuration;

namespace FinTrack.Infrastructure.Jobs;

public sealed record BackgroundJobSettings(bool Enabled, int IntervalSeconds, int MaxAttempts,
    int RetryDelayMilliseconds, int MaxUsersPerCycle)
{
    public static BackgroundJobSettings Load(IConfiguration configuration)
    {
        var settings = new BackgroundJobSettings(configuration.GetValue("BackgroundJobs:Enabled", false),
            configuration.GetValue("BackgroundJobs:IntervalSeconds", 60),
            configuration.GetValue("BackgroundJobs:MaxAttempts", 3),
            configuration.GetValue("BackgroundJobs:RetryDelayMilliseconds", 500),
            configuration.GetValue("BackgroundJobs:MaxUsersPerCycle", 100));
        if (settings.IntervalSeconds is < 1 or > 86400 || settings.MaxAttempts is < 1 or > 5
            || settings.RetryDelayMilliseconds is < 50 or > 10000 || settings.MaxUsersPerCycle is < 1 or > 1000)
            throw new InvalidOperationException("BackgroundJobs possui intervalo, retries ou limite fora dos valores permitidos.");
        return settings;
    }
}
