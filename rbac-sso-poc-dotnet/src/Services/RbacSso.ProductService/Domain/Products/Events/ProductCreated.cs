using RbacSso.Common.Domain;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a new product is created.
/// </summary>
public record ProductCreated(
    Guid ProductId,
    string ProductCode,
    string Name,
    decimal Price,
    string Category,
    string TenantId,
    string CreatedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_CREATED";
}
