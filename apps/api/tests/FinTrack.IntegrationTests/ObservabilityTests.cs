using System.Collections.Concurrent;
using System.Diagnostics;
using FinTrack.Api.Observability;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace FinTrack.IntegrationTests;

public sealed class ObservabilityTests : IAsyncLifetime
{
    private readonly ConcurrentQueue<Activity> exported = new();
    private PostgresApiFixture fixture = null!;
    private sealed class Capture(ConcurrentQueue<Activity> activities) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch) activities.Enqueue(activity);
            return ExportResult.Success;
        }
    }
    public async Task InitializeAsync()
    {
        fixture = new() { ConfigureServices = services => services.ConfigureOpenTelemetryTracerProvider(
            (_, tracing) => tracing.AddProcessor(new SimpleActivityExportProcessor(new Capture(exported)))) };
        await fixture.InitializeAsync();
    }
    public Task DisposeAsync() => fixture.DisposeAsync();

    [Fact]
    public async Task UnauthorizedAndUnmatchedRequests_ReturnTraceId_AndExportSafeSpans()
    {
        using var client = fixture.CreateClient();
        var traceId = ActivityTraceId.CreateRandom();
        client.DefaultRequestHeaders.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");
        using var unauthorized = await client.GetAsync("/transactions?secret=never-export-this");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(traceId.ToString(), Assert.Single(unauthorized.Headers.GetValues("X-Correlation-ID")));
        using var missing = await client.GetAsync("/private-value-never-export-this");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
        var spans = exported.Where(activity => activity.TraceId == traceId && activity.Kind == ActivityKind.Server).ToArray();
        Assert.Equal(2, spans.Length);
        Assert.All(spans, activity =>
        {
            Assert.DoesNotContain("never-export-this", activity.DisplayName);
            Assert.All(activity.TagObjects, tag => Assert.DoesNotContain("never-export-this", tag.Value?.ToString() ?? ""));
        });
        Assert.Contains(spans, activity => Equals(activity.GetTagItem("http.response.status_code"), 401));
    }
    [Fact]
    public void Processor_RemovesSqlCredentialsAndErrorDescriptions()
    {
        using var activity = new Activity("SELECT secret").Start();
        activity.SetTag("db.query.text", "secret sql").SetTag("url.full", "http://user:password@local?secret=value")
            .SetTag("http.request.header.authorization", "Bearer token").SetTag("db.system.name", "postgresql");
        activity.SetStatus(ActivityStatusCode.Error, "secret error");
        new SafeTraceProcessor().OnEnd(activity);
        Assert.Null(activity.GetTagItem("db.query.text"));
        Assert.Null(activity.GetTagItem("url.full"));
        Assert.Null(activity.GetTagItem("http.request.header.authorization"));
        Assert.Equal("postgresql", activity.GetTagItem("db.system.name"));
        Assert.Null(activity.StatusDescription);
    }
}
