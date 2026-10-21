using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace FinTrack.Api.Observability;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? ActivityTraceId.CreateRandom().ToString();
        context.Response.Headers["X-Correlation-ID"] = traceId;
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId });
        var started = Stopwatch.GetTimestamp();
        var failed = false;
        try { await next(context); }
        catch { failed = true; throw; }
        finally
        {
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            logger.LogInformation("HTTP {Method} {Route} completed {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method, route, failed ? 500 : context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
