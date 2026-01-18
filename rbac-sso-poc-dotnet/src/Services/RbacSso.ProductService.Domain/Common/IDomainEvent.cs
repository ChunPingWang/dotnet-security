namespace RbacSso.ProductService.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// 領域事件標記介面
///
/// Note: This interface has NO external dependencies.
/// MediatR integration is done in Application/Infrastructure layers.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When this event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
