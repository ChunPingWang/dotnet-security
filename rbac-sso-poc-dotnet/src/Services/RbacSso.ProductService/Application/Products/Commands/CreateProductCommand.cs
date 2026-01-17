using MediatR;
using RbacSso.ProductService.Application.Common.Interfaces;
using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products;
using RbacSso.Security.Authentication;
using RbacSso.Tenant;

namespace RbacSso.ProductService.Application.Products.Commands;

/// <summary>
/// Command to create a new product.
/// </summary>
public record CreateProductCommand(
    string? ProductCode,
    string Name,
    decimal Price,
    string Category,
    string? Description
) : IRequest<Guid>;

/// <summary>
/// Handler for CreateProductCommand.
/// </summary>
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public CreateProductHandler(
        IProductRepository repository,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IMediator mediator)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var productCode = string.IsNullOrEmpty(request.ProductCode)
            ? Domain.Products.ProductCode.Generate()
            : Domain.Products.ProductCode.Create(request.ProductCode);

        // Check for duplicate product code
        if (await _repository.ExistsByCodeAsync(productCode, cancellationToken))
        {
            // Retry with a new code
            productCode = Domain.Products.ProductCode.Generate();
            if (await _repository.ExistsByCodeAsync(productCode, cancellationToken))
            {
                throw new Common.Exceptions.BusinessRuleException("PRD-B00003",
                    "Unable to generate unique product code. Please try again.");
            }
        }

        var product = Product.Create(
            productCode,
            request.Name,
            Money.Create(request.Price),
            request.Category,
            request.Description,
            _tenantContext.TenantId,
            _currentUser.Username
        );

        await _repository.AddAsync(product, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in product.PullDomainEvents())
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return product.Id.Value;
    }
}
