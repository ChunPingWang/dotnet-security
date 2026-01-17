using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RbacSso.Common.Exceptions;
using RbacSso.Common.Responses;

namespace RbacSso.Common.Middleware;

/// <summary>
/// Global exception handler middleware.
/// 全域例外處理中介軟體
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context.TraceIdentifier;

        var (statusCode, errorResponse) = exception switch
        {
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                ErrorResponse.Create(domainEx.Code, domainEx.Message, correlationId)
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ErrorResponse.Create("AUTH-E00001", "Unauthorized access", correlationId)
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                ErrorResponse.Create("SYS-N00001", "Resource not found", correlationId)
            ),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                ErrorResponse.Create("SYS-V00001", argEx.Message, correlationId)
            ),
            InvalidOperationException invEx => (
                HttpStatusCode.BadRequest,
                ErrorResponse.Create("SYS-B00001", invEx.Message, correlationId)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                ErrorResponse.Create("SYS-E00001", "An unexpected error occurred", correlationId)
            )
        };

        // Log the exception
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}",
                correlationId, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception,
                "Handled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, StatusCode: {StatusCode}",
                correlationId, context.Request.Path, (int)statusCode);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
