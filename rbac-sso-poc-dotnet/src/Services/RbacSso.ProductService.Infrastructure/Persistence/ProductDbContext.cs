using Microsoft.EntityFrameworkCore;
using RbacSso.ProductService.Application.Ports;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework DbContext for Product Service.
/// Implements multi-tenant data isolation via global query filters.
/// </summary>
public class ProductDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public ProductDbContext(
        DbContextOptions<ProductDbContext> options,
        ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);

        // Multi-tenant query filter
        // ADMIN can see all tenants, others see only their tenant
        modelBuilder.Entity<Product>().HasQueryFilter(p =>
            _currentUser.IsAdmin || p.TenantId == _currentUser.TenantId);
    }
}
