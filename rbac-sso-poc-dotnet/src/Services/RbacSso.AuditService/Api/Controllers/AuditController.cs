using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSso.AuditService.Application.Queries;
using RbacSso.Common.Responses;
using RbacSso.Security.Authorization;

namespace RbacSso.AuditService.Api.Controllers;

/// <summary>
/// Controller for Audit Log read operations.
/// 審計日誌讀取操作控制器
/// </summary>
/// <remarks>
/// Audit logs are immutable. No create, update, or delete operations are exposed.
/// Write operations are handled internally via domain events.
/// </remarks>
[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a list of audit logs with filtering and pagination.
    /// 取得審計日誌列表 (支援篩選、分頁)
    /// </summary>
    /// <remarks>
    /// Required roles: ADMIN, TENANT_ADMIN
    /// TENANT_ADMIN can only see their tenant's logs.
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? eventType,
        [FromQuery] string? username,
        [FromQuery] string? correlationId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page < 0) page = 0;
        if (size < 1) size = 1;
        if (size > 100) size = 100;

        var query = new ListAuditLogsQuery(
            eventType,
            username,
            correlationId,
            fromDate,
            toDate,
            page,
            size
        );

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Success(result));
    }

    /// <summary>
    /// Gets an audit log by ID.
    /// 依 ID 取得審計日誌
    /// </summary>
    /// <remarks>
    /// Required roles: ADMIN, TENANT_ADMIN
    /// </remarks>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin}")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAuditLogByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
        {
            return NotFound(ErrorResponse.NotFound("AUDIT-N00001", $"Audit log with ID {id} not found"));
        }

        return Ok(ApiResponse<AuditLogDto>.Success(result));
    }

    /// <summary>
    /// Gets audit logs by correlation ID.
    /// 依關聯 ID 取得審計日誌
    /// </summary>
    /// <remarks>
    /// Required roles: ADMIN, TENANT_ADMIN
    /// Useful for tracing related operations across services.
    /// </remarks>
    [HttpGet("correlation/{correlationId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCorrelationId(
        string correlationId,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListAuditLogsQuery(
            CorrelationId: correlationId,
            Page: page,
            Size: size
        );

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Success(result));
    }

    /// <summary>
    /// Audit logs cannot be modified (HTTP 405 Method Not Allowed).
    /// 審計日誌不可修改
    /// </summary>
    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
    public IActionResult UpdateNotAllowed(Guid id)
    {
        return StatusCode(StatusCodes.Status405MethodNotAllowed,
            ErrorResponse.Create("AUDIT-M00001", "Audit logs are immutable and cannot be modified"));
    }

    /// <summary>
    /// Audit logs cannot be deleted via API (HTTP 405 Method Not Allowed).
    /// 審計日誌無法透過 API 刪除
    /// </summary>
    /// <remarks>
    /// Audit logs are only deleted by the 90-day retention policy.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
    public IActionResult DeleteNotAllowed(Guid id)
    {
        return StatusCode(StatusCodes.Status405MethodNotAllowed,
            ErrorResponse.Create("AUDIT-M00002", "Audit logs cannot be deleted via API. Retention policy handles cleanup."));
    }
}
