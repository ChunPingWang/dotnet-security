using RbacSso.ProductService.Application.Ports;
using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products;

namespace RbacSso.ProductService.Application.Products.Commands;

/// <summary>
/// Command to update an existing product.
/// 更新產品命令
/// </summary>
public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    decimal Price,
    string Category,
    string? Description
);

/// <summary>
/// Use case handler for updating a product.
/// 更新產品用例處理器
/// </summary>
public sealed class UpdateProductHandler
{
    private readonly IProductRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UpdateProductHandler(
        IProductRepository repository,
        ICurrentUserService currentUser,
        IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _currentUser = currentUser;
        _eventDispatcher = eventDispatcher;
    }

    public async Task HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var productId = ProductId.From(command.ProductId);
        var product = await _repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
        {
            throw new DomainException("PRD-N00001", $"Product with ID {command.ProductId} not found");
        }

        product.Update(
            command.Name,
            Money.Create(command.Price),
            command.Category,
            command.Description,
            _currentUser.Username);

        _repository.Update(product);
        await _repository.SaveChangesAsync(cancellationToken);

        // Dispatch domain events
        await _eventDispatcher.DispatchAsync(product.DomainEvents, cancellationToken);
        product.ClearDomainEvents();
    }
}
