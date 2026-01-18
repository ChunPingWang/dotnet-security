using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a product is updated.
/// 產品更新領域事件
/// </summary>
public sealed record ProductUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public Guid ProductId { get; init; }
    public string Name { get; init; } = null!;
    public decimal Price { get; init; }
    public string Currency { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string UpdatedBy { get; init; } = null!;
}
