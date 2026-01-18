using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Product entity.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => ProductId.From(value))
            .HasColumnName("id");

        builder.Property(p => p.Code)
            .HasConversion(
                code => code.Value,
                value => ProductCode.Create(value))
            .HasColumnName("product_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("ix_products_code");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsOne(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("price")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("USD")
                .IsRequired();
        });

        builder.Property(p => p.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("ix_products_category");

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("ix_products_tenant_id");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        // Composite indexes
        builder.HasIndex(p => new { p.TenantId, p.Category, p.Status })
            .HasDatabaseName("ix_products_tenant_category_status");

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}
