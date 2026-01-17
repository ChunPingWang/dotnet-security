using RbacSso.Common.Domain;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a product is updated.
/// </summary>
public record ProductUpdated(
    Guid ProductId,
    string Name,
    decimal Price,
    string Category,
    string UpdatedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_UPDATED";
}
