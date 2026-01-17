using MediatR;

namespace RbacSso.Common.Domain;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// The type of the event (e.g., "PRODUCT_CREATED", "PRODUCT_UPDATED").
    /// </summary>
    string EventType { get; }
}
