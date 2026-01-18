using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSso.ProductService.Application.Products.Commands;
using RbacSso.ProductService.Application.Products.Queries;

namespace RbacSso.ProductService.Api.Controllers;

/// <summary>
/// Product API Controller.
/// 產品 API 控制器
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly CreateProductHandler _createHandler;
    private readonly UpdateProductHandler _updateHandler;
    private readonly DeleteProductHandler _deleteHandler;
    private readonly GetProductByIdHandler _getByIdHandler;
    private readonly ListProductsHandler _listHandler;

    public ProductsController(
        CreateProductHandler createHandler,
        UpdateProductHandler updateHandler,
        DeleteProductHandler deleteHandler,
        GetProductByIdHandler getByIdHandler,
        ListProductsHandler listHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getByIdHandler = getByIdHandler;
        _listHandler = listHandler;
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,TENANT_ADMIN,USER")]
    [ProducesResponseType(typeof(CreateProductResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.ProductCode,
            request.Name,
            request.Price,
            request.Category,
            request.Description);

        var result = await _createHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.ProductId },
            result);
    }

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMIN,TENANT_ADMIN,USER,VIEWER")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _getByIdHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists products with pagination, filtering, and sorting.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,TENANT_ADMIN,USER,VIEWER")]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListProductsQuery(category, sortBy, descending, page, size);
        var result = await _listHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN,TENANT_ADMIN,USER")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.Price,
            request.Category,
            request.Description);

        await _updateHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN,TENANT_ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);
        await _deleteHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }
}

public record CreateProductRequest(
    string? ProductCode,
    string Name,
    decimal Price,
    string Category,
    string? Description);

public record UpdateProductRequest(
    string Name,
    decimal Price,
    string Category,
    string? Description);
