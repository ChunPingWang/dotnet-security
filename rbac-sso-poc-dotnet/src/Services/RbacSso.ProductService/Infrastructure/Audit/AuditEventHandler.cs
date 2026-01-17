using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using RbacSso.Audit;
using RbacSso.Audit.Domain;
using RbacSso.Common.Domain;
using RbacSso.ProductService.Domain.Products.Events;

namespace RbacSso.ProductService.Infrastructure.Audit;

/// <summary>
/// Handles domain events and creates audit log entries.
/// 處理領域事件並建立審計日誌
/// </summary>
public class AuditEventHandler :
    INotificationHandler<ProductCreated>,
    INotificationHandler<ProductUpdated>,
    INotificationHandler<ProductPriceChanged>,
    INotificationHandler<ProductDeleted>
{
    private readonly IAuditLogRepository _auditRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string ServiceName = "ProductService";

    public AuditEventHandler(
        IAuditLogRepository auditRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditRepository = auditRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task Handle(ProductCreated notification, CancellationToken cancellationToken)
    {
        var auditLog = CreateAuditLog(
            notification,
            "PRODUCT_CREATED",
            "CREATE",
            notification.ProductId.ToString());

        await _auditRepository.AddAsync(auditLog, cancellationToken);
        await _auditRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ProductUpdated notification, CancellationToken cancellationToken)
    {
        var auditLog = CreateAuditLog(
            notification,
            "PRODUCT_UPDATED",
            "UPDATE",
            notification.ProductId.ToString());

        await _auditRepository.AddAsync(auditLog, cancellationToken);
        await _auditRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ProductPriceChanged notification, CancellationToken cancellationToken)
    {
        var auditLog = CreateAuditLog(
            notification,
            "PRODUCT_PRICE_CHANGED",
            "UPDATE",
            notification.ProductId.ToString());

        await _auditRepository.AddAsync(auditLog, cancellationToken);
        await _auditRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ProductDeleted notification, CancellationToken cancellationToken)
    {
        var auditLog = CreateAuditLog(
            notification,
            "PRODUCT_DELETED",
            "DELETE",
            notification.ProductId.ToString());

        await _auditRepository.AddAsync(auditLog, cancellationToken);
        await _auditRepository.SaveChangesAsync(cancellationToken);
    }

    private AuditLog CreateAuditLog(
        IDomainEvent domainEvent,
        string eventType,
        string action,
        string aggregateId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var username = httpContext?.User?.Identity?.Name ?? "system";
        var clientIp = GetClientIp(httpContext);
        var correlationId = GetCorrelationId(httpContext);
        var payload = JsonSerializer.Serialize(domainEvent);

        return AuditLog.Create(
            eventType: eventType,
            aggregateType: "Product",
            aggregateId: aggregateId,
            username: username,
            serviceName: ServiceName,
            action: action,
            payload: payload,
            result: AuditResult.Success,
            clientIp: clientIp,
            correlationId: correlationId);
    }

    private static string? GetClientIp(HttpContext? httpContext)
    {
        if (httpContext is null) return null;

        // Check for forwarded header (behind proxy)
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
}
