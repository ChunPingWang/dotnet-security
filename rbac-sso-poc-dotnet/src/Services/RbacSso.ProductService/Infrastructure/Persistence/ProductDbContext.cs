using Microsoft.EntityFrameworkCore;
using RbacSso.ProductService.Domain.Products;
using RbacSso.Tenant;

namespace RbacSso.ProductService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework DbContext for Product Service.
/// Implements multi-tenant data isolation via global query filters.
/// </summary>
public class ProductDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ProductDbContext(
        DbContextOptions<ProductDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);

        // Multi-tenant query filter
        // ADMIN role users can see all tenants, others see only their tenant
        modelBuilder.Entity<Product>().HasQueryFilter(p =>
            _tenantContext.IsAdmin || p.TenantId == _tenantContext.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Ensure tenant ID is set on new entities
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = _tenantContext.TenantId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
