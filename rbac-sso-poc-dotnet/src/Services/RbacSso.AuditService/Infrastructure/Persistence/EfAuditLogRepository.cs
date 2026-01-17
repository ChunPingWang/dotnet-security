using Microsoft.EntityFrameworkCore;
using RbacSso.Audit;
using RbacSso.Audit.Domain;

namespace RbacSso.AuditService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework implementation of IAuditLogRepository.
/// </summary>
public class EfAuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _context;

    public EfAuditLogRepository(AuditDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string? username = null,
        string? eventType = null,
        string? correlationId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int page = 0,
        int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(a => a.Username == username);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(a => a.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(a => a.CorrelationId == correlationId);
        }

        if (from.HasValue)
        {
            query = query.Where(a => a.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.Timestamp <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(page * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<AuditLog>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.CorrelationId == correlationId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
