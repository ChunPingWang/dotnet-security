using MediatR;
using RbacSso.Audit;
using RbacSso.Audit.Domain;

namespace RbacSso.AuditService.Application.Queries;

/// <summary>
/// Query to list audit logs with filtering and pagination.
/// </summary>
public record ListAuditLogsQuery(
    string? EventType = null,
    string? Username = null,
    string? CorrelationId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int Page = 0,
    int Size = 20
) : IRequest<PagedResult<AuditLogDto>>;

/// <summary>
/// Handler for ListAuditLogsQuery.
/// </summary>
public class ListAuditLogsHandler : IRequestHandler<ListAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAuditLogRepository _repository;

    public ListAuditLogsHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(ListAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.Username,
            request.EventType,
            request.CorrelationId,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.Size,
            cancellationToken
        );

        var dtos = items.Select(AuditLogDto.FromDomain).ToList();

        return new PagedResult<AuditLogDto>(
            dtos,
            totalCount,
            request.Page,
            request.Size
        );
    }
}

/// <summary>
/// Audit log DTO for query responses.
/// </summary>
public record AuditLogDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string EventType,
    string AggregateType,
    string AggregateId,
    string Username,
    string ServiceName,
    string Action,
    string Result,
    string? ErrorMessage,
    string? ClientIp,
    string CorrelationId,
    bool PayloadTruncated
)
{
    public static AuditLogDto FromDomain(AuditLog log)
    {
        return new AuditLogDto(
            log.Id,
            log.Timestamp,
            log.EventType,
            log.AggregateType,
            log.AggregateId,
            log.Username,
            log.ServiceName,
            log.Action,
            log.Result.ToString(),
            log.ErrorMessage,
            log.ClientIp,
            log.CorrelationId,
            log.PayloadTruncated
        );
    }
}

/// <summary>
/// Generic paged result wrapper.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int Size
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Size);
    public bool HasPreviousPage => Page > 0;
    public bool HasNextPage => Page < TotalPages - 1;
}
