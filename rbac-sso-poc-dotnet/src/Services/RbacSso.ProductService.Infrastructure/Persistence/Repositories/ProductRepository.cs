using Microsoft.EntityFrameworkCore;
using RbacSso.ProductService.Application.Ports;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IProductRepository.
/// ADAPTER in Hexagonal Architecture.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.Code == code, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
