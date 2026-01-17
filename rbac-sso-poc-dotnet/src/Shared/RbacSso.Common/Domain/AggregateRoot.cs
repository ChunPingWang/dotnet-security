namespace RbacSso.Common.Domain;

/// <summary>
/// Base class for aggregate roots in DDD.
/// Aggregates are clusters of domain objects that can be treated as a single unit.
/// The aggregate root is the only entry point for accessing the aggregate.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class AggregateRoot<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// The unique identifier for this aggregate.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// The domain events that have been registered but not yet published.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a domain event to be published after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to register.</param>
    protected void RegisterDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Pulls all registered domain events and clears the internal list.
    /// This should be called after persisting the aggregate.
    /// </summary>
    /// <returns>The list of domain events that were registered.</returns>
    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>
    /// Clears all registered domain events without returning them.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
