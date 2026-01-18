using Microsoft.EntityFrameworkCore;
using RbacSso.ProductService.Application.Ports;
using RbacSso.ProductService.Application.Products.Queries;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IProductQueryRepository.
/// Optimized for read operations (CQRS Query side).
/// </summary>
public sealed class ProductQueryRepository : IProductQueryRepository
{
    private readonly ProductDbContext _context;

    public ProductQueryRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.Id == ProductId.From(id))
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Code.Value,
                p.Name,
                p.Price.Amount,
                p.Price.Currency,
                p.Category,
                p.Description,
                p.Status.ToString(),
                p.TenantId,
                p.CreatedBy,
                p.CreatedAt,
                p.UpdatedBy,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(
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

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, sortBy, descending);

        // Apply pagination and project to DTO
        var items = await query
            .Skip(page * size)
            .Take(size)
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Code.Value,
                p.Name,
                p.Price.Amount,
                p.Price.Currency,
                p.Category,
                p.Description,
                p.Status.ToString(),
                p.TenantId,
                p.CreatedBy,
                p.CreatedAt,
                p.UpdatedBy,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, page, size);
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
            "category" => descending
                ? query.OrderByDescending(p => p.Category)
                : query.OrderBy(p => p.Category),
            "createdat" or "created_at" => descending
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }
}
