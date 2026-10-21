using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace FinTrack.Infrastructure.Observability;

public static class FinTrackTelemetry
{
    public const string Name = "FinTrack";
    public static readonly ActivitySource Activities = new(Name, "1.0.0");
    public static readonly Meter Metrics = new(Name, "1.0.0");
    public static readonly Counter<long> Published = Metrics.CreateCounter<long>("fintrack.messaging.published", "{event}");
    public static readonly Counter<long> Consumed = Metrics.CreateCounter<long>("fintrack.messaging.consumed", "{delivery}");
    public static readonly Counter<long> Failures = Metrics.CreateCounter<long>("fintrack.messaging.failures", "{failure}");
    public static readonly Counter<long> DeadLetters = Metrics.CreateCounter<long>("fintrack.messaging.dead_letters", "{delivery}");
    public static readonly Counter<long> JobCycles = Metrics.CreateCounter<long>("fintrack.recurring.cycles", "{cycle}");
    public static readonly Counter<long> Generated = Metrics.CreateCounter<long>("fintrack.recurring.generated", "{transaction}");

    public static ActivityContext Extract(string? parent, string? state)
        => ActivityContext.TryParse(parent, state?.Length <= 512 ? state : null, true, out var context) ? context : default;

    public static string? Header(IDictionary<string, object?>? headers, string name)
        => headers?.TryGetValue(name, out var value) == true && value is byte[] bytes && bytes.Length <= 512
            ? Encoding.UTF8.GetString(bytes) : null;

    public static Dictionary<string, object?> Headers(Activity? activity)
    {
        var headers = new Dictionary<string, object?>();
        if (activity?.Id is not null) headers["traceparent"] = Encoding.UTF8.GetBytes(activity.Id);
        if (activity?.TraceStateString is { Length: > 0 and <= 512 } state) headers["tracestate"] = Encoding.UTF8.GetBytes(state);
        return headers;
    }
}
