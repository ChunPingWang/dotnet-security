using MediatR;
using RbacSso.ProductService.Application.Common.Exceptions;
using RbacSso.ProductService.Application.Common.Interfaces;
using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products;
using RbacSso.Security.Authentication;

namespace RbacSso.ProductService.Application.Products.Commands;

/// <summary>
/// Command to update an existing product.
/// </summary>
public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    decimal Price,
    string Category,
    string? Description
) : IRequest<Unit>;

/// <summary>
/// Handler for UpdateProductCommand.
/// </summary>
public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly IProductRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public UpdateProductHandler(
        IProductRepository repository,
        ICurrentUser currentUser,
        IMediator mediator)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var productId = ProductId.From(request.ProductId);
        var product = await _repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("PRD-N00001", $"Product with ID {request.ProductId} not found");
        }

        product.Update(
            request.Name,
            Money.Create(request.Price),
            request.Category,
            request.Description,
            _currentUser.Username
        );

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
