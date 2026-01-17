using MediatR;
using RbacSso.ProductService.Application.Common.Exceptions;
using RbacSso.ProductService.Application.Common.Interfaces;
using RbacSso.ProductService.Domain.Products;
using RbacSso.Security.Authentication;

namespace RbacSso.ProductService.Application.Products.Commands;

/// <summary>
/// Command to soft-delete a product.
/// </summary>
public record DeleteProductCommand(Guid ProductId) : IRequest<Unit>;

/// <summary>
/// Handler for DeleteProductCommand.
/// </summary>
public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IProductRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public DeleteProductHandler(
        IProductRepository repository,
        ICurrentUser currentUser,
        IMediator mediator)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var productId = ProductId.From(request.ProductId);
        var product = await _repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("PRD-N00001", $"Product with ID {request.ProductId} not found");
        }

        product.Delete(_currentUser.Username);

        _repository.Update(product);
        await _repository.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in product.PullDomainEvents())
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return Unit.Value;
    }
}
