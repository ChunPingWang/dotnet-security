using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSso.Common.Responses;
using RbacSso.ProductService.Application.Products.Queries;
using RbacSso.Security.Authorization;

namespace RbacSso.ProductService.Api.Controllers;

/// <summary>
/// Controller for Product read operations (Queries).
/// 產品讀取操作控制器
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductQueryController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductQueryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a product by ID.
    /// 依 ID 取得產品
    /// </summary>
    /// <remarks>
    /// Required roles: All authenticated users
    /// </remarks>
    [HttpGet("{id:guid}", Name = "GetProductById")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin},{Roles.User},{Roles.Viewer}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<ProductDto>.Success(product));
    }

    /// <summary>
    /// Gets a list of products with pagination, filtering, and sorting.
    /// 取得產品列表 (支援分頁、篩選、排序)
    /// </summary>
    /// <remarks>
    /// Required roles: All authenticated users
    ///
    /// Sort options: name, price, createdAt, category
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin},{Roles.User},{Roles.Viewer}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page < 0) page = 0;
        if (size < 1) size = 1;
        if (size > 100) size = 100; // Max page size

        var query = new ListProductsQuery(category, sortBy, descending, page, size);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<PagedResult<ProductDto>>.Success(result));
    }
}
