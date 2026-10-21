using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Infrastructure.Jobs;

public static class BackgroundJobRegistration
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = BackgroundJobSettings.Load(configuration);
        services.AddSingleton(settings);
        services.AddSingleton<IRecurringJobProcessor, RecurringJobProcessor>();
        if (settings.Enabled) services.AddHostedService<RecurringTransactionsWorker>();
        return services;
    }
}
