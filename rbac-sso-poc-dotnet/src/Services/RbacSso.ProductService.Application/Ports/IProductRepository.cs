using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Application.Ports;

/// <summary>
/// Port interface for Product persistence.
/// 產品持久化 Port 介面
///
/// This is a PORT in Hexagonal Architecture.
/// Implementation (ADAPTER) is in Infrastructure layer.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);
    Task<Product?> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(ProductCode code, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
