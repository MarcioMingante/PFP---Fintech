using System.Diagnostics;
using FinTrack.Infrastructure.Observability;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FinTrack.Api.Observability;

public static class TelemetryRegistration
{
    public static IServiceCollection AddFinTrackTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Observability:Enabled", true)) return services;
        var endpoint = configuration["Observability:OtlpEndpoint"];
        Uri? uri = null;
        if (!string.IsNullOrWhiteSpace(endpoint) && (!Uri.TryCreate(endpoint, UriKind.Absolute, out uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)))
            throw new InvalidOperationException("Observability:OtlpEndpoint deve ser uma URL HTTP sem credenciais.");
        services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService("fintrack-api"))
            .WithTracing(tracing =>
            {
                tracing.AddSource(FinTrackTelemetry.Name, "Npgsql")
                    .AddAspNetCoreInstrumentation(options => options.RecordException = false)
                    .AddProcessor(new SafeTraceProcessor());
                if (uri is not null) tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = uri; options.Protocol = OtlpExportProtocol.Grpc; options.TimeoutMilliseconds = 2000;
                });
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(FinTrackTelemetry.Name, "System.Runtime")
                    .AddAspNetCoreInstrumentation();
                if (uri is not null) metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = uri; options.Protocol = OtlpExportProtocol.Grpc; options.TimeoutMilliseconds = 2000;
                });
            });
        return services;
    }
}

// Export only operational tags; never SQL, URLs/query strings, headers or financial payloads.
public sealed class SafeTraceProcessor : BaseProcessor<Activity>
{
    private static readonly HashSet<string> Allowed = ["http.request.method", "http.response.status_code", "http.route",
        "network.protocol.version", "url.scheme", "db.system", "db.system.name", "db.operation.name",
        "messaging.system", "messaging.destination.name", "messaging.operation.name", "error.type"];
    public override void OnEnd(Activity activity)
    {
        foreach (var tag in activity.TagObjects.ToArray())
            if (!Allowed.Contains(tag.Key)) activity.SetTag(tag.Key, null);
        if (activity.Source.Name == "Npgsql") activity.DisplayName = "PostgreSQL operation";
        if (activity.Kind == ActivityKind.Server)
            activity.DisplayName = $"{activity.GetTagItem("http.request.method") ?? "HTTP"} {activity.GetTagItem("http.route") ?? "unmatched"}";
        if (activity.Status == ActivityStatusCode.Error) activity.SetStatus(ActivityStatusCode.Error);
    }
}
