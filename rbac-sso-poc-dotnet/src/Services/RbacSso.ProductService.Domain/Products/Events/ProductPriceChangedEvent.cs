using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a product's price changes.
/// 產品價格變更領域事件
/// </summary>
public sealed record ProductPriceChangedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public Guid ProductId { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public string Currency { get; init; } = null!;
    public string ChangedBy { get; init; } = null!;
}
