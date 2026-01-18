using RbacSso.ProductService.Application.Ports;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Application.Products.Commands;

/// <summary>
/// Command to create a new product.
/// 建立產品命令
/// </summary>
public sealed record CreateProductCommand(
    string? ProductCode,
    string Name,
    decimal Price,
    string Category,
    string? Description
);

/// <summary>
/// Result of CreateProductCommand.
/// </summary>
public sealed record CreateProductResult(Guid ProductId, string ProductCode);

/// <summary>
/// Use case handler for creating a product.
/// 建立產品用例處理器
/// </summary>
public sealed class CreateProductHandler
{
    private readonly IProductRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateProductHandler(
        IProductRepository repository,
        ICurrentUserService currentUser,
        IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _currentUser = currentUser;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<CreateProductResult> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        // Generate or validate product code
        var productCode = string.IsNullOrEmpty(command.ProductCode)
            ? ProductCode.Generate()
            : ProductCode.Create(command.ProductCode);

        // Check for duplicate code
        if (await _repository.ExistsByCodeAsync(productCode, cancellationToken))
        {
            // Retry with new generated code
            productCode = ProductCode.Generate();
            if (await _repository.ExistsByCodeAsync(productCode, cancellationToken))
            {
                throw new Domain.Common.BusinessRuleException("PRD-B00003",
                    "Unable to generate unique product code. Please try again.");
            }
        }

        // Create product aggregate
        var product = Product.Create(
            productCode,
            command.Name,
            Money.Create(command.Price),
            command.Category,
            command.Description,
            _currentUser.TenantId,
            _currentUser.Username);

        // Persist
        await _repository.AddAsync(product, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Dispatch domain events
        await _eventDispatcher.DispatchAsync(product.DomainEvents, cancellationToken);
        product.ClearDomainEvents();

        return new CreateProductResult(product.Id.Value, product.Code.Value);
    }
}
