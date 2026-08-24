using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace TarotApi.Middleware;

public sealed partial class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    public const string RequestIdHeaderName = "X-Request-ID";
    private const int MaxRequestIdLength = 128;

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeRequestIdPattern();

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        context.TraceIdentifier = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestIdHeaderName] = requestId;
            return Task.CompletedTask;
        });

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["request_id"] = requestId
        });

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";

            logger.LogInformation(
                "HTTP request completed: {Method} {Route} {StatusCode} in {DurationMs} ms",
                context.Request.Method,
                route,
                context.Response.StatusCode,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2));
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out var values)
            && values.Count == 1)
        {
            var candidate = values[0];
            if (!string.IsNullOrWhiteSpace(candidate)
                && candidate.Length <= MaxRequestIdLength
                && SafeRequestIdPattern().IsMatch(candidate))
            {
                return candidate;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
