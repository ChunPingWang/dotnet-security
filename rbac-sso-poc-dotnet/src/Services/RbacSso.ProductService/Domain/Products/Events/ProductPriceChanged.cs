using RbacSso.Common.Domain;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a product's price changes.
/// This is raised in addition to ProductUpdated when price specifically changes.
/// </summary>
public record ProductPriceChanged(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string ChangedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_PRICE_CHANGED";
}
