using FluentAssertions;
using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products;
using RbacSso.ProductService.Domain.Products.Events;
using Xunit;

namespace RbacSso.ProductService.Domain.UnitTests.Products;

public class ProductTests
{
    private const string TenantId = "tenant-a";
    private const string Username = "test-user";

    [Fact]
    public void Create_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var code = ProductCode.Generate();
        var name = "Test Product";
        var price = Money.Create(99.99m);
        var category = "Electronics";

        // Act
        var product = Product.Create(code, name, price, category, "Description", TenantId, Username);

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBeNull();
        product.Code.Should().Be(code);
        product.Name.Should().Be(name);
        product.Price.Should().Be(price);
        product.Category.Should().Be(category);
        product.Status.Should().Be(ProductStatus.Active);
        product.TenantId.Should().Be(TenantId);
        product.CreatedBy.Should().Be(Username);
    }

    [Fact]
    public void Create_ShouldRaiseProductCreatedEvent()
    {
        // Arrange & Act
        var product = CreateTestProduct();

        // Assert
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedEvent>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowValidationException()
    {
        // Arrange & Act
        var act = () => Product.Create(
            ProductCode.Generate(), "", Money.Create(10), "Cat", null, TenantId, Username);

        // Assert
        act.Should().Throw<ValidationException>()
            .Where(e => e.Code == "PRD-V00005");
    }

    [Fact]
    public void Create_WithLongName_ShouldThrowValidationException()
    {
        // Arrange
        var longName = new string('a', 201);

        // Act
        var act = () => Product.Create(
            ProductCode.Generate(), longName, Money.Create(10), "Cat", null, TenantId, Username);

        // Assert
        act.Should().Throw<ValidationException>()
            .Where(e => e.Code == "PRD-V00006");
    }

    [Fact]
    public void Update_ShouldUpdateProperties()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();

        // Act
        product.Update("Updated Name", Money.Create(199.99m), "Updated Cat", "New Desc", "updater");

        // Assert
        product.Name.Should().Be("Updated Name");
        product.Price.Amount.Should().Be(199.99m);
        product.Category.Should().Be("Updated Cat");
        product.UpdatedBy.Should().Be("updater");
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_ShouldRaiseProductUpdatedEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();

        // Act
        product.Update("New Name", Money.Create(100), "Cat", null, "updater");

        // Assert
        product.DomainEvents.Should().Contain(e => e is ProductUpdatedEvent);
    }

    [Fact]
    public void Update_WithPriceChange_ShouldRaisePriceChangedEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        var oldPrice = product.Price.Amount;
        product.ClearDomainEvents();

        // Act
        product.Update("Same Name", Money.Create(oldPrice + 50), "Cat", null, "updater");

        // Assert
        product.DomainEvents.Should().Contain(e => e is ProductPriceChangedEvent);
        var priceEvent = product.DomainEvents.OfType<ProductPriceChangedEvent>().First();
        priceEvent.OldPrice.Should().Be(oldPrice);
        priceEvent.NewPrice.Should().Be(oldPrice + 50);
    }

    [Fact]
    public void Update_WithoutPriceChange_ShouldNotRaisePriceChangedEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        var samePrice = product.Price.Amount;
        product.ClearDomainEvents();

        // Act
        product.Update("New Name", Money.Create(samePrice), "Cat", null, "updater");

        // Assert
        product.DomainEvents.Should().NotContain(e => e is ProductPriceChangedEvent);
    }

    [Fact]
    public void Delete_ShouldSetStatusToDeleted()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.Delete("deleter");

        // Assert
        product.Status.Should().Be(ProductStatus.Deleted);
        product.UpdatedBy.Should().Be("deleter");
    }

    [Fact]
    public void Delete_ShouldRaiseProductDeletedEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();

        // Act
        product.Delete("deleter");

        // Assert
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductDeletedEvent>();
    }

    [Fact]
    public void Update_WhenDeleted_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Delete("deleter");

        // Act
        var act = () => product.Update("Name", Money.Create(10), "Cat", null, "updater");

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .Where(e => e.Code == "PRD-B00002");
    }

    [Fact]
    public void Activate_WhenDeleted_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Delete("deleter");

        // Act
        var act = () => product.Activate("activator");

        // Assert
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Deactivate_ShouldSetStatusToInactive()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.Deactivate("deactivator");

        // Assert
        product.Status.Should().Be(ProductStatus.Inactive);
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Deactivate("deactivator");

        // Act
        product.Activate("activator");

        // Assert
        product.Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var product = CreateTestProduct();
        product.DomainEvents.Should().NotBeEmpty();

        // Act
        product.ClearDomainEvents();

        // Assert
        product.DomainEvents.Should().BeEmpty();
    }

    private static Product CreateTestProduct()
    {
        return Product.Create(
            ProductCode.Generate(),
            "Test Product",
            Money.Create(99.99m),
            "Test Category",
            "Test Description",
            TenantId,
            Username);
    }
}
