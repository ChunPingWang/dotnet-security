using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Application.Ports;

/// <summary>
/// Port interface for dispatching domain events.
/// 領域事件發送 Port 介面
///
/// Implementation will use MediatR or other messaging infrastructure.
/// Domain layer remains free of external dependencies.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
