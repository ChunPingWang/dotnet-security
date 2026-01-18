using RbacSso.ProductService.Application.Ports;

namespace RbacSso.ProductService.Application.Products.Queries;

/// <summary>
/// Query to list products with pagination, filtering, and sorting.
/// 列出產品查詢
/// </summary>
public sealed record ListProductsQuery(
    string? Category = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 0,
    int Size = 20
);

/// <summary>
/// Use case handler for listing products.
/// 列出產品用例處理器
/// </summary>
public sealed class ListProductsHandler
{
    private readonly IProductQueryRepository _queryRepository;

    public ListProductsHandler(IProductQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<PagedResult<ProductDto>> HandleAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        var page = Math.Max(0, query.Page);
        var size = Math.Clamp(query.Size, 1, 100);

        return await _queryRepository.GetPagedAsync(
            query.Category,
            query.SortBy,
            query.Descending,
            page,
            size,
            cancellationToken);
    }
}
