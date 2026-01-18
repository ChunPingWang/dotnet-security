using MediatR;
using RbacSso.ProductService.Application.Ports;
using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Infrastructure.Events;

/// <summary>
/// MediatR implementation of IDomainEventDispatcher.
/// Wraps domain events in MediatR notifications for dispatch.
/// </summary>
public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public MediatRDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Wrap domain event in MediatR notification
        var notification = new DomainEventNotification(domainEvent);
        await _mediator.Publish(notification, cancellationToken);
    }
}

/// <summary>
/// MediatR notification wrapper for domain events.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
