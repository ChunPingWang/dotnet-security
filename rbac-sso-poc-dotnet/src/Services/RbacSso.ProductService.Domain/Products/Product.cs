using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products.Events;

namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Product Aggregate Root.
/// 產品聚合根
///
/// Invariants:
/// - ProductCode is unique within the system
/// - Price must be non-negative
/// - Name cannot be empty
/// - TenantId is immutable after creation
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public ProductCode Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProductStatus Status { get; private set; }
    public string TenantId { get; private set; } = null!;

    // Audit fields
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product() { } // For ORM

    /// <summary>
    /// Factory method to create a new Product.
    /// </summary>
    public static Product Create(
        ProductCode code,
        string name,
        Money price,
        string category,
        string? description,
        string tenantId,
        string createdBy)
    {
        ValidateName(name);
        ValidateCategory(category);
        ValidateTenantId(tenantId);

        var product = new Product
        {
            Id = ProductId.Create(),
            Code = code,
            Name = name.Trim(),
            Price = price,
            Category = category.Trim(),
            Description = description?.Trim(),
            Status = ProductStatus.Active,
            TenantId = tenantId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent
        {
            ProductId = product.Id.Value,
            ProductCode = product.Code.Value,
            Name = product.Name,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            Category = product.Category,
            TenantId = product.TenantId,
            CreatedBy = product.CreatedBy
        });

        return product;
    }

    /// <summary>
    /// Updates the product's information.
    /// </summary>
    public void Update(string name, Money price, string category, string? description, string updatedBy)
    {
        ValidateName(name);
        ValidateCategory(category);
        EnsureNotDeleted();

        var oldPrice = Price;

        Name = name.Trim();
        Price = price;
        Category = category.Trim();
        Description = description?.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ProductUpdatedEvent
        {
            ProductId = Id.Value,
            Name = Name,
            Price = Price.Amount,
            Currency = Price.Currency,
            Category = Category,
            UpdatedBy = updatedBy
        });

        // Raise price changed event if price actually changed
        if (oldPrice.Amount != price.Amount)
        {
            RaiseDomainEvent(new ProductPriceChangedEvent
            {
                ProductId = Id.Value,
                OldPrice = oldPrice.Amount,
                NewPrice = price.Amount,
                Currency = price.Currency,
                ChangedBy = updatedBy
            });
        }
    }

    /// <summary>
    /// Soft-deletes the product.
    /// </summary>
    public void Delete(string deletedBy)
    {
        EnsureNotDeleted();

        Status = ProductStatus.Deleted;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ProductDeletedEvent
        {
            ProductId = Id.Value,
            DeletedBy = deletedBy
        });
    }

    /// <summary>
    /// Activates the product.
    /// </summary>
    public void Activate(string activatedBy)
    {
        EnsureNotDeleted();

        Status = ProductStatus.Active;
        UpdatedBy = activatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deactivates the product.
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        EnsureNotDeleted();

        Status = ProductStatus.Inactive;
        UpdatedBy = deactivatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureNotDeleted()
    {
        if (Status == ProductStatus.Deleted)
            throw new BusinessRuleException("PRD-B00002", "Cannot modify a deleted product");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("PRD-V00005", "Product name cannot be empty");

        if (name.Length > 200)
            throw new ValidationException("PRD-V00006", "Product name cannot exceed 200 characters");
    }

    private static void ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ValidationException("PRD-V00007", "Product category cannot be empty");

        if (category.Length > 100)
            throw new ValidationException("PRD-V00008", "Product category cannot exceed 100 characters");
    }

    private static void ValidateTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ValidationException("PRD-V00009", "Tenant ID cannot be empty");
    }
}
