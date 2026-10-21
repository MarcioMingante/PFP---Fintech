using System.Diagnostics;
using System.Text;
using FinTrack.Infrastructure.Observability;

namespace FinTrack.UnitTests.Observability;

public sealed class PropagationTests
{
    [Fact]
    public void W3cHeaders_RoundTripTraceAndState()
    {
        using var activity = new Activity("test").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.TraceStateString = "vendor=value";
        var headers = FinTrackTelemetry.Headers(activity);
        var context = FinTrackTelemetry.Extract(FinTrackTelemetry.Header(headers, "traceparent"), FinTrackTelemetry.Header(headers, "tracestate"));
        Assert.Equal(activity.TraceId, context.TraceId);
        Assert.Equal(activity.SpanId, context.SpanId);
        Assert.Equal("vendor=value", context.TraceState);
        Assert.True(context.IsRemote);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public void InvalidOrMissingParent_ReturnsEmptyContext(string? parent)
        => Assert.Equal(default, FinTrackTelemetry.Extract(parent, null));
    [Fact]
    public void OversizedOrWrongTypeHeader_IsIgnored()
    {
        Assert.Null(FinTrackTelemetry.Header(new Dictionary<string, object?> { ["traceparent"] = Encoding.UTF8.GetBytes(new string('a', 513)) }, "traceparent"));
        Assert.Null(FinTrackTelemetry.Header(new Dictionary<string, object?> { ["traceparent"] = 123 }, "traceparent"));
    }
}
