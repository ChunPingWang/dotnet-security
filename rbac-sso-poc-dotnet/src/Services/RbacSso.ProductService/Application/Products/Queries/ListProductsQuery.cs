using MediatR;
using RbacSso.ProductService.Application.Common.Interfaces;

namespace RbacSso.ProductService.Application.Products.Queries;

/// <summary>
/// Query to list products with pagination, filtering, and sorting.
/// </summary>
public record ListProductsQuery(
    string? Category = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 0,
    int Size = 20
) : IRequest<PagedResult<ProductDto>>;

/// <summary>
/// Handler for ListProductsQuery.
/// </summary>
public class ListProductsHandler : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _repository;

    public ListProductsHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.Category,
            request.SortBy,
            request.Descending,
            request.Page,
            request.Size,
            cancellationToken
        );

        var dtos = items.Select(ProductDto.FromDomain).ToList();

        return new PagedResult<ProductDto>(
            dtos,
            totalCount,
            request.Page,
            request.Size
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
