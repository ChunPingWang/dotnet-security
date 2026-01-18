using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Domain.Products.Events;

/// <summary>
/// Domain event raised when a product is created.
/// 產品建立領域事件
/// </summary>
public sealed record ProductCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = null!;
    public string Name { get; init; } = null!;
    public decimal Price { get; init; }
    public string Currency { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string TenantId { get; init; } = null!;
    public string CreatedBy { get; init; } = null!;
}
