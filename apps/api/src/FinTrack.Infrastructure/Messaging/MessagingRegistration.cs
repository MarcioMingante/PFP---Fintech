using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Infrastructure.Messaging;

public sealed record MessagingSettings(bool Enabled, string? ConnectionString);

public static class MessagingRegistration
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("Messaging:Enabled");
        var connection = configuration.GetConnectionString("RabbitMq");
        if (enabled && (!Uri.TryCreate(connection, UriKind.Absolute, out var uri) || uri.Scheme is not ("amqp" or "amqps")))
            throw new InvalidOperationException("ConnectionStrings:RabbitMq deve ser uma URI AMQP válida.");
        services.AddSingleton(new MessagingSettings(enabled, connection));
        if (enabled) services.AddHostedService<MessagingWorker>();
        return services;
    }
}
