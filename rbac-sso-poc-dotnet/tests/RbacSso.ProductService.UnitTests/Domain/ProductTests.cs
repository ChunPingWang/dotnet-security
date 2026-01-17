using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products;
using Xunit;

namespace RbacSso.ProductService.UnitTests.Domain;

/// <summary>
/// Unit tests for Product aggregate root.
/// 產品聚合根的單元測試
/// </summary>
public class ProductTests
{
    private const string TestTenantId = "tenant-a";
    private const string TestUsername = "test-user";

    [Fact]
    public void Create_WithValidParameters_ShouldCreateProduct()
    {
        // Arrange
        var productCode = ProductCode.Generate();
        var name = "Test Product";
        var price = Money.Create(99.99m);
        var category = "Electronics";
        var description = "A test product";

        // Act
        var product = Product.Create(
            productCode,
            name,
            price,
            category,
            description,
            TestTenantId,
            TestUsername);

        // Assert
        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product.Id.Value);
        Assert.Equal(productCode, product.ProductCode);
        Assert.Equal(name, product.Name);
        Assert.Equal(price.Amount, product.Price.Amount);
        Assert.Equal(category, product.Category);
        Assert.Equal(description, product.Description);
        Assert.Equal(ProductStatus.Active, product.Status);
        Assert.Equal(TestTenantId, product.TenantId);
        Assert.Equal(TestUsername, product.CreatedBy);
    }

    [Fact]
    public void Create_ShouldRaiseProductCreatedEvent()
    {
        // Arrange
        var productCode = ProductCode.Generate();

        // Act
        var product = Product.Create(
            productCode,
            "Test Product",
            Money.Create(99.99m),
            "Electronics",
            null,
            TestTenantId,
            TestUsername);

        // Assert
        var domainEvents = product.PullDomainEvents();
        Assert.Single(domainEvents);
        var createdEvent = Assert.IsType<RbacSso.ProductService.Domain.Products.Events.ProductCreated>(domainEvents.First());
        Assert.Equal(product.Id.Value, createdEvent.ProductId);
        Assert.Equal(productCode.Value, createdEvent.ProductCode);
    }

    [Fact]
    public void Update_ShouldUpdateProductProperties()
    {
        // Arrange
        var product = CreateTestProduct();
        var newName = "Updated Product";
        var newPrice = Money.Create(149.99m);
        var newCategory = "Updated Category";
        var newDescription = "Updated description";
        var updatedBy = "updater";

        // Act
        product.Update(newName, newPrice, newCategory, newDescription, updatedBy);

        // Assert
        Assert.Equal(newName, product.Name);
        Assert.Equal(newPrice.Amount, product.Price.Amount);
        Assert.Equal(newCategory, product.Category);
        Assert.Equal(newDescription, product.Description);
        Assert.Equal(updatedBy, product.UpdatedBy);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldRaiseProductUpdatedEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.PullDomainEvents(); // Clear creation event

        // Act
        product.Update("Updated", Money.Create(100m), "Cat", null, "updater");

        // Assert
        var domainEvents = product.PullDomainEvents();
        Assert.Contains(domainEvents, e => e is RbacSso.ProductService.Domain.Products.Events.ProductUpdated);
    }

    [Fact]
    public void Update_WithPriceChange_ShouldRaisePriceChangedEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        var originalPrice = product.Price.Amount;
        product.PullDomainEvents(); // Clear creation event

        // Act
        product.Update("Same Name", Money.Create(199.99m), "Same Cat", null, "updater");

        // Assert
        var domainEvents = product.PullDomainEvents();
        var priceChangedEvent = domainEvents.OfType<RbacSso.ProductService.Domain.Products.Events.ProductPriceChanged>().FirstOrDefault();
        Assert.NotNull(priceChangedEvent);
        Assert.Equal(originalPrice, priceChangedEvent.OldPrice);
        Assert.Equal(199.99m, priceChangedEvent.NewPrice);
    }

    [Fact]
    public void Delete_ShouldSetStatusToDeleted()
    {
        // Arrange
        var product = CreateTestProduct();
        var deletedBy = "deleter";

        // Act
        product.Delete(deletedBy);

        // Assert
        Assert.Equal(ProductStatus.Deleted, product.Status);
        Assert.Equal(deletedBy, product.UpdatedBy);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void Delete_ShouldRaiseProductDeletedEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.PullDomainEvents(); // Clear creation event

        // Act
        product.Delete("deleter");

        // Assert
        var domainEvents = product.PullDomainEvents();
        Assert.Single(domainEvents);
        Assert.IsType<RbacSso.ProductService.Domain.Products.Events.ProductDeleted>(domainEvents.First());
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetStatusToActive()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Deactivate("deactivator");

        // Act
        product.Activate("activator");

        // Assert
        Assert.Equal(ProductStatus.Active, product.Status);
    }

    [Fact]
    public void Activate_WhenDeleted_ShouldThrowException()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Delete("deleter");

        // Act & Assert
        Assert.Throws<RbacSso.ProductService.Domain.Common.Exceptions.BusinessRuleException>(
            () => product.Activate("activator"));
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetStatusToInactive()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.Deactivate("deactivator");

        // Assert
        Assert.Equal(ProductStatus.Inactive, product.Status);
    }

    [Fact]
    public void Deactivate_WhenDeleted_ShouldThrowException()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Delete("deleter");

        // Act & Assert
        Assert.Throws<RbacSso.ProductService.Domain.Common.Exceptions.BusinessRuleException>(
            () => product.Deactivate("deactivator"));
    }

    [Fact]
    public void PullDomainEvents_ShouldClearEvents()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var firstPull = product.PullDomainEvents();
        var secondPull = product.PullDomainEvents();

        // Assert
        Assert.NotEmpty(firstPull);
        Assert.Empty(secondPull);
    }

    private static Product CreateTestProduct()
    {
        return Product.Create(
            ProductCode.Generate(),
            "Test Product",
            Money.Create(99.99m),
            "Test Category",
            "Test Description",
            TestTenantId,
            TestUsername);
    }
}
