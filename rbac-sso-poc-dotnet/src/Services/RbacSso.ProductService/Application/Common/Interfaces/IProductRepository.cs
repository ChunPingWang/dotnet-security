using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Product aggregate persistence.
/// This is a port in the Hexagonal Architecture.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Adds a new product to the repository.
    /// </summary>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its ID.
    /// </summary>
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its code.
    /// </summary>
    Task<Product?> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product code already exists.
    /// </summary>
    Task<bool> ExistsByCodeAsync(ProductCode code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with optional filtering and pagination.
    /// </summary>
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? category = null,
        string? sortBy = null,
        bool descending = false,
        int page = 0,
        int size = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    void Update(Product product);

    /// <summary>
    /// Saves changes to the repository.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
