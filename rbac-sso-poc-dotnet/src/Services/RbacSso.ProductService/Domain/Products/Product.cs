using RbacSso.Common.Domain;
using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products.Events;
using RbacSso.Tenant;

namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Product Aggregate Root.
/// Represents a product in the e-commerce catalog.
/// </summary>
public class Product : AggregateRoot<ProductId>, ITenantEntity
{
    public ProductCode ProductCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProductStatus Status { get; private set; }
    public string TenantId { get; private set; } = null!;
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product() { } // For EF Core

    /// <summary>
    /// Creates a new Product aggregate.
    /// </summary>
    public static Product Create(
        ProductCode productCode,
        string name,
        Money price,
        string category,
        string? description,
        string tenantId,
        string createdBy)
    {
        var product = new Product
        {
            Id = ProductId.Create(),
            ProductCode = productCode,
            Name = name,
            Price = price,
            Category = category,
            Description = description,
            Status = ProductStatus.Active,
            TenantId = tenantId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        product.RegisterDomainEvent(new ProductCreated(
            product.Id.Value,
            product.ProductCode.Value,
            product.Name,
            product.Price.Amount,
            product.Category,
            product.TenantId,
            product.CreatedBy
        ));

        return product;
    }

    /// <summary>
    /// Updates the product's information.
    /// </summary>
    public void Update(string name, Money price, string category, string? description, string updatedBy)
    {
        var oldPrice = Price;

        Name = name;
        Price = price;
        Category = category;
        Description = description;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;

        RegisterDomainEvent(new ProductUpdated(
            Id.Value,
            Name,
            Price.Amount,
            Category,
            updatedBy
        ));

        // If price changed, raise additional event
        if (oldPrice.Amount != price.Amount)
        {
            RegisterDomainEvent(new ProductPriceChanged(
                Id.Value,
                oldPrice.Amount,
                price.Amount,
                updatedBy
            ));
        }
    }

    /// <summary>
    /// Soft deletes the product.
    /// </summary>
    public void Delete(string deletedBy)
    {
        Status = ProductStatus.Deleted;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTimeOffset.UtcNow;

        RegisterDomainEvent(new ProductDeleted(Id.Value, deletedBy));
    }

    /// <summary>
    /// Activates the product.
    /// </summary>
    public void Activate(string activatedBy)
    {
        if (Status == ProductStatus.Deleted)
        {
            throw new Common.Exceptions.BusinessRuleException("PRD-B00001",
                "Cannot activate a deleted product");
        }

        Status = ProductStatus.Active;
        UpdatedBy = activatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deactivates the product.
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        if (Status == ProductStatus.Deleted)
        {
            throw new Common.Exceptions.BusinessRuleException("PRD-B00002",
                "Cannot deactivate a deleted product");
        }

        Status = ProductStatus.Inactive;
        UpdatedBy = deactivatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
