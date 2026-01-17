namespace RbacSso.Audit.Domain;

/// <summary>
/// Represents an audit log entry for tracking significant operations.
/// Audit logs are immutable once created and provide a complete trail of system activity.
/// </summary>
public class AuditLog
{
    private const int MaxPayloadSize = 10 * 1024; // 10KB

    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// When the audited event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// The type of event (e.g., "PRODUCT_CREATED", "USER_LOGGED_IN").
    /// </summary>
    public string EventType { get; private set; } = null!;

    /// <summary>
    /// The type of aggregate affected (e.g., "Product", "User").
    /// </summary>
    public string AggregateType { get; private set; } = null!;

    /// <summary>
    /// The ID of the aggregate affected.
    /// </summary>
    public string AggregateId { get; private set; } = null!;

    /// <summary>
    /// The username of the user who performed the action.
    /// </summary>
    public string Username { get; private set; } = null!;

    /// <summary>
    /// The name of the service where the action originated.
    /// </summary>
    public string ServiceName { get; private set; } = null!;

    /// <summary>
    /// The action performed (e.g., "CREATE", "UPDATE", "DELETE").
    /// </summary>
    public string Action { get; private set; } = null!;

    /// <summary>
    /// JSON serialized payload containing event details.
    /// </summary>
    public string Payload { get; private set; } = null!;

    /// <summary>
    /// The result of the operation.
    /// </summary>
    public AuditResult Result { get; private set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The IP address of the client that initiated the request.
    /// </summary>
    public string? ClientIp { get; private set; }

    /// <summary>
    /// Correlation ID for tracing related operations.
    /// </summary>
    public string CorrelationId { get; private set; } = null!;

    /// <summary>
    /// Indicates if the payload was truncated due to size limits.
    /// </summary>
    public bool PayloadTruncated { get; private set; }

    private AuditLog() { } // For EF Core

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    public static AuditLog Create(
        string eventType,
        string aggregateType,
        string aggregateId,
        string username,
        string serviceName,
        string action,
        string payload,
        AuditResult result,
        string? clientIp,
        string correlationId,
        string? errorMessage = null)
    {
        var payloadTruncated = payload.Length > MaxPayloadSize;
        var truncatedPayload = payloadTruncated
            ? payload[..MaxPayloadSize] + "... [TRUNCATED]"
            : payload;

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Username = username,
            ServiceName = serviceName,
            Action = action,
            Payload = truncatedPayload,
            Result = result,
            ErrorMessage = errorMessage,
            ClientIp = clientIp,
            CorrelationId = correlationId,
            PayloadTruncated = payloadTruncated
        };
    }
}
