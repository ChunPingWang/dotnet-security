using RbacSso.ProductService.Application.Products.Queries;

namespace RbacSso.ProductService.Application.Ports;

/// <summary>
/// Port interface for Product read operations (CQRS Query side).
/// 產品讀取操作 Port 介面
///
/// Separated from IProductRepository for CQRS pattern.
/// Returns DTOs directly for optimized read operations.
/// </summary>
public interface IProductQueryRepository
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductDto>> GetPagedAsync(
        string? category = null,
        string? sortBy = null,
        bool descending = false,
        int page = 0,
        int size = 20,
        CancellationToken cancellationToken = default);
}
