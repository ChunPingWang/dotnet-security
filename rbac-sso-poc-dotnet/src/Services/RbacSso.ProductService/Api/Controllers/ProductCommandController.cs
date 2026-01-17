using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSso.Common.Responses;
using RbacSso.ProductService.Application.Products.Commands;
using RbacSso.Security.Authorization;

namespace RbacSso.ProductService.Api.Controllers;

/// <summary>
/// Controller for Product write operations (Commands).
/// 產品寫入操作控制器
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductCommandController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductCommandController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new product.
    /// 建立新產品
    /// </summary>
    /// <remarks>
    /// Required roles: ADMIN, TENANT_ADMIN, USER
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin},{Roles.User}")]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.ProductCode,
            request.Name,
            request.Price,
            request.Category,
            request.Description
        );

        var productId = await _mediator.Send(command, cancellationToken);

        var response = ApiResponse<CreateProductResponse>.Success(
            new CreateProductResponse(productId),
            "Product created successfully"
        );

        return CreatedAtRoute(
            "GetProductById",
            new { id = productId },
            response
        );
    }

    /// <summary>
    /// Updates an existing product.
    /// 更新現有產品
    /// </summary>
    /// <remarks>
    /// Required roles: ADMIN, TENANT_ADMIN, USER
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin},{Roles.User}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.Price,
            request.Category,
            request.Description
        );

        await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<object>.Success(null, "Product updated successfully"));
    }

    /// <summary>
    /// Soft-deletes a product.
    /// 軟刪除產品
    /// </summary>
    /// <remarks>
    /// Required roles: ADMIN, TENANT_ADMIN
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.TenantAdmin}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

/// <summary>
/// Request DTO for creating a product.
/// </summary>
public record CreateProductRequest(
    string? ProductCode,
    string Name,
    decimal Price,
    string Category,
    string? Description
);

/// <summary>
/// Response DTO for product creation.
/// </summary>
public record CreateProductResponse(Guid ProductId);

/// <summary>
/// Request DTO for updating a product.
/// </summary>
public record UpdateProductRequest(
    string Name,
    decimal Price,
    string Category,
    string? Description
);
