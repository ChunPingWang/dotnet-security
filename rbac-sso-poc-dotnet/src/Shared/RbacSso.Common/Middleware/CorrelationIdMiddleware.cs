using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace RbacSso.Common.Middleware;

/// <summary>
/// Middleware that ensures each request has a correlation ID for tracing.
/// The correlation ID is either extracted from the X-Correlation-ID header
/// or generated if not present.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        correlationIdAccessor.SetCorrelationId(correlationId);

        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Add correlation ID to Serilog log context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId)
            && !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Interface for accessing the current request's correlation ID.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// The correlation ID for the current request.
    /// </summary>
    string CorrelationId { get; }
}

/// <summary>
/// Scoped service that holds the correlation ID for the current request.
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private string _correlationId = string.Empty;

    public string CorrelationId => _correlationId;

    internal void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
    }
}
