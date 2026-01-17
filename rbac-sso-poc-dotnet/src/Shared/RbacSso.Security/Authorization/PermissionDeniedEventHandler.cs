using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RbacSso.Audit;
using RbacSso.Audit.Domain;

namespace RbacSso.Security.Authorization;

/// <summary>
/// Handles permission denied events and creates audit log entries.
/// 處理權限拒絕事件並建立審計日誌
/// </summary>
public class PermissionDeniedEventHandler
{
    private readonly IAuditLogRepository _auditRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionDeniedEventHandler(
        IAuditLogRepository auditRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditRepository = auditRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Logs a permission denied event.
    /// </summary>
    public async Task LogPermissionDeniedAsync(
        string resourceType,
        string resourceId,
        string action,
        string[] requiredRoles,
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var username = httpContext?.User?.Identity?.Name ?? "anonymous";
        var clientIp = GetClientIp(httpContext);
        var correlationId = GetCorrelationId(httpContext);

        var payload = JsonSerializer.Serialize(new
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            RequiredRoles = requiredRoles,
            UserRoles = GetUserRoles(httpContext),
            Reason = "Insufficient permissions"
        });

        var auditLog = AuditLog.Create(
            eventType: "PERMISSION_DENIED",
            aggregateType: resourceType,
            aggregateId: resourceId,
            username: username,
            serviceName: serviceName,
            action: action,
            payload: payload,
            result: AuditResult.Denied,
            clientIp: clientIp,
            correlationId: correlationId);

        await _auditRepository.AddAsync(auditLog, cancellationToken);
        await _auditRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Logs a cross-tenant access attempt.
    /// </summary>
    public async Task LogCrossTenantAccessAttemptAsync(
        string resourceType,
        string resourceId,
        string attemptedTenantId,
        string targetTenantId,
        string action,
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var username = httpContext?.User?.Identity?.Name ?? "anonymous";
        var clientIp = GetClientIp(httpContext);
        var correlationId = GetCorrelationId(httpContext);

        var payload = JsonSerializer.Serialize(new
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            AttemptedTenantId = attemptedTenantId,
            TargetTenantId = targetTenantId,
            Action = action,
            Reason = "Cross-tenant access attempt blocked"
        });

        var auditLog = AuditLog.Create(
            eventType: "CROSS_TENANT_ACCESS_ATTEMPT",
            aggregateType: resourceType,
            aggregateId: resourceId,
            username: username,
            serviceName: serviceName,
            action: action,
            payload: payload,
            result: AuditResult.Denied,
            clientIp: clientIp,
            correlationId: correlationId);

        await _auditRepository.AddAsync(auditLog, cancellationToken);
        await _auditRepository.SaveChangesAsync(cancellationToken);
    }

    private static string? GetClientIp(HttpContext? httpContext)
    {
        if (httpContext is null) return null;

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string GetCorrelationId(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return Guid.NewGuid().ToString();
        }

        return httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? httpContext.TraceIdentifier
            ?? Guid.NewGuid().ToString();
    }

    private static string[] GetUserRoles(HttpContext? httpContext)
    {
        if (httpContext?.User is null) return Array.Empty<string>();

        return httpContext.User.Claims
            .Where(c => c.Type == "realm_access.roles" || c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();
    }
}
