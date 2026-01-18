using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a product is deleted.
/// 產品刪除領域事件
/// </summary>
public sealed record ProductDeletedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public Guid ProductId { get; init; }
    public string DeletedBy { get; init; } = null!;
}
