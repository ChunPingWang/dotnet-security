namespace RbacSso.Common.Domain;

/// <summary>
/// Base record for all domain events.
/// Provides common properties like EventId and OccurredAt timestamp.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public abstract string EventType { get; }
}
