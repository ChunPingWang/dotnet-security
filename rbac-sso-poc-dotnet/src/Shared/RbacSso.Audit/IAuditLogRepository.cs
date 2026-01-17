using RbacSso.Audit.Domain;

namespace RbacSso.Audit;

/// <summary>
/// Repository interface for audit log persistence.
/// This is a port interface in the Hexagonal Architecture.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an audit log entry by ID.
    /// </summary>
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with optional filtering and pagination.
    /// </summary>
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string? username = null,
        string? eventType = null,
        string? correlationId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int page = 0,
        int size = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by correlation ID.
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the repository.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
