using System.Diagnostics;

namespace EmployeeApi.Middleware;

/// <summary>
/// Custom middleware providing structured request and response logging for Kubernetes diagnostics.
/// Captures execution duration, status codes, and pod context for Azure Monitor / Container Insights.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly string _podName;

    public RequestLoggingMiddleware(RequestDelegate _next, ILogger<RequestLoggingMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
        _podName = Environment.GetEnvironmentVariable("POD_NAME")
            ?? Environment.GetEnvironmentVariable("HOSTNAME")
            ?? Environment.MachineName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip noisy health checks from filling standard request logs if desired, or log them at Trace/Debug
        var path = context.Request.Path.Value ?? string.Empty;
        var isHealthCheck = path.Equals("/health", StringComparison.OrdinalIgnoreCase)
                         || path.Equals("/ready", StringComparison.OrdinalIgnoreCase);

        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;

        try
        {
            await _next(context);
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            if (isHealthCheck)
            {
                _logger.LogTrace(
                    "Health probe: {Method} {Path} responded {StatusCode} in {ElapsedMs:0.000} ms [Pod: {PodName}]",
                    method, path, statusCode, elapsedMs, _podName);
            }
            else
            {
                var logLevel = statusCode >= 500 ? LogLevel.Error :
                               statusCode >= 400 ? LogLevel.Warning :
                               LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.000} ms [Pod: {PodName}, TraceId: {TraceId}]",
                    method, path, statusCode, elapsedMs, _podName, context.TraceIdentifier);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            _logger.LogError(
                ex,
                "Unhandled error during HTTP {Method} {Path} after {ElapsedMs:0.000} ms [Pod: {PodName}, TraceId: {TraceId}]: {ErrorMessage}",
                method, path, elapsedMs, _podName, context.TraceIdentifier, ex.Message);

            throw; // Re-throw to allow standard ProblemDetails / ExceptionHandler middleware to format HTTP 500
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}

