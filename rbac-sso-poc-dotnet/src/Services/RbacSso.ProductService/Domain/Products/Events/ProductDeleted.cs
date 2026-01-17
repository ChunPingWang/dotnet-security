using RbacSso.Common.Domain;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a product is deleted (soft delete).
/// </summary>
public record ProductDeleted(
    Guid ProductId,
    string DeletedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_DELETED";
}
