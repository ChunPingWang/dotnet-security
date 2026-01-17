using MediatR;
using RbacSso.ProductService.Application.Common.Exceptions;
using RbacSso.ProductService.Application.Common.Interfaces;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Application.Products.Queries;

/// <summary>
/// Query to get a product by its ID.
/// </summary>
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;

/// <summary>
/// Handler for GetProductByIdQuery.
/// </summary>
public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _repository;

    public GetProductByIdHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var productId = ProductId.From(request.ProductId);
        var product = await _repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("PRD-N00001", $"Product with ID {request.ProductId} not found");
        }

        return ProductDto.FromDomain(product);
    }
}

/// <summary>
/// Product DTO for query responses.
/// </summary>
public record ProductDto(
    Guid Id,
    string ProductCode,
    string Name,
    decimal Price,
    string Category,
    string? Description,
    string Status,
    string TenantId,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string? UpdatedBy,
    DateTimeOffset? UpdatedAt
)
{
    public static ProductDto FromDomain(Product product)
    {
        return new ProductDto(
            product.Id.Value,
            product.ProductCode.Value,
            product.Name,
            product.Price.Amount,
            product.Category,
            product.Description,
            product.Status.ToString(),
            product.TenantId,
            product.CreatedBy,
            product.CreatedAt,
            product.UpdatedBy,
            product.UpdatedAt
        );
    }
}
