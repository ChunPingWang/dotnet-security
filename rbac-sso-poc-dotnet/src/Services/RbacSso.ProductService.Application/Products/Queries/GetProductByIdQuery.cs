using RbacSso.ProductService.Application.Ports;
using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Application.Products.Queries;

/// <summary>
/// Query to get a product by ID.
/// 依 ID 取得產品查詢
/// </summary>
public sealed record GetProductByIdQuery(Guid ProductId);

/// <summary>
/// Use case handler for getting a product by ID.
/// 依 ID 取得產品用例處理器
/// </summary>
public sealed class GetProductByIdHandler
{
    private readonly IProductQueryRepository _queryRepository;

    public GetProductByIdHandler(IProductQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<ProductDto> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var product = await _queryRepository.GetByIdAsync(query.ProductId, cancellationToken);

        if (product is null)
        {
            throw new DomainException("PRD-N00001", $"Product with ID {query.ProductId} not found");
        }

        return product;
    }
}
