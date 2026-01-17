using MediatR;
using RbacSso.Audit;

namespace RbacSso.AuditService.Application.Queries;

/// <summary>
/// Query to get an audit log by its ID.
/// </summary>
public record GetAuditLogByIdQuery(Guid Id) : IRequest<AuditLogDto?>;

/// <summary>
/// Handler for GetAuditLogByIdQuery.
/// </summary>
public class GetAuditLogByIdHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogDto?>
{
    private readonly IAuditLogRepository _repository;

    public GetAuditLogByIdHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<AuditLogDto?> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
    {
        var log = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return log is null ? null : AuditLogDto.FromDomain(log);
    }
}
