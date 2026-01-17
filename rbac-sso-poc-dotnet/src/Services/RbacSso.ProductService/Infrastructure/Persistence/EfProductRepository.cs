using Microsoft.EntityFrameworkCore;
using RbacSso.ProductService.Application.Common.Interfaces;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework implementation of IProductRepository.
/// </summary>
public class EfProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public EfProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.ProductCode == code, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.ProductCode == code, cancellationToken);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? category = null,
        string? sortBy = null,
        bool descending = false,
        int page = 0,
        int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Where(p => p.Status != ProductStatus.Deleted)
            .AsQueryable();

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, sortBy, descending);

        // Apply pagination
        var items = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplySorting(
        IQueryable<Product> query,
        string? sortBy,
        bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "price" => descending
                ? query.OrderByDescending(p => p.Price.Amount)
                : query.OrderBy(p => p.Price.Amount),
            "createdat" or "created_at" => descending
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            "category" => descending
                ? query.OrderByDescending(p => p.Category)
                : query.OrderBy(p => p.Category),
            _ => query.OrderByDescending(p => p.CreatedAt) // Default sort
        };
    }
}
