using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products;
using Xunit;

namespace RbacSso.ProductService.UnitTests.Domain;

/// <summary>
/// Unit tests for value objects.
/// 值物件的單元測試
/// </summary>
public class ValueObjectTests
{
    #region ProductId Tests

    [Fact]
    public void ProductId_Create_ShouldGenerateUniqueId()
    {
        // Act
        var id1 = ProductId.Create();
        var id2 = ProductId.Create();

        // Assert
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
        Assert.NotEqual(id1.Value, id2.Value);
    }

    [Fact]
    public void ProductId_From_ShouldCreateFromGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var productId = ProductId.From(guid);

        // Assert
        Assert.Equal(guid, productId.Value);
    }

    [Fact]
    public void ProductId_Equality_ShouldBeValueBased()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = ProductId.From(guid);
        var id2 = ProductId.From(guid);

        // Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
    }

    #endregion

    #region ProductCode Tests

    [Fact]
    public void ProductCode_Generate_ShouldCreateValidCode()
    {
        // Act
        var code = ProductCode.Generate();

        // Assert
        Assert.NotNull(code.Value);
        Assert.StartsWith("PRD-", code.Value);
        Assert.Matches(@"^PRD-[A-Z0-9]{8}$", code.Value);
    }

    [Fact]
    public void ProductCode_Create_WithValidCode_ShouldSucceed()
    {
        // Arrange
        var codeString = "PRD-ABC12345";

        // Act
        var code = ProductCode.Create(codeString);

        // Assert
        Assert.Equal(codeString, code.Value);
    }

    [Fact]
    public void ProductCode_Create_WithInvalidCode_ShouldThrowException()
    {
        // Arrange
        var invalidCodes = new[] { "", "   ", "ABC", "PRD-", "invalid-code" };

        // Act & Assert
        foreach (var invalidCode in invalidCodes)
        {
            Assert.Throws<ArgumentException>(() => ProductCode.Create(invalidCode));
        }
    }

    [Fact]
    public void ProductCode_Equality_ShouldBeValueBased()
    {
        // Arrange
        var codeString = "PRD-TEST1234";
        var code1 = ProductCode.Create(codeString);
        var code2 = ProductCode.Create(codeString);

        // Assert
        Assert.Equal(code1, code2);
        Assert.True(code1 == code2);
    }

    #endregion

    #region Money Tests

    [Fact]
    public void Money_Create_WithValidAmount_ShouldSucceed()
    {
        // Act
        var money = Money.Create(99.99m);

        // Assert
        Assert.Equal(99.99m, money.Amount);
        Assert.Equal("USD", money.Currency); // Default currency
    }

    [Fact]
    public void Money_Create_WithCurrency_ShouldSucceed()
    {
        // Act
        var money = Money.Create(100m, "EUR");

        // Assert
        Assert.Equal(100m, money.Amount);
        Assert.Equal("EUR", money.Currency);
    }

    [Fact]
    public void Money_Create_WithNegativeAmount_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Money.Create(-10m));
    }

    [Fact]
    public void Money_Create_WithZeroAmount_ShouldSucceed()
    {
        // Act
        var money = Money.Create(0m);

        // Assert
        Assert.Equal(0m, money.Amount);
    }

    [Fact]
    public void Money_Equality_ShouldBeValueBased()
    {
        // Arrange
        var money1 = Money.Create(99.99m, "USD");
        var money2 = Money.Create(99.99m, "USD");
        var money3 = Money.Create(99.99m, "EUR");

        // Assert
        Assert.Equal(money1, money2);
        Assert.NotEqual(money1, money3); // Different currency
    }

    [Fact]
    public void Money_Add_ShouldReturnNewMoney()
    {
        // Arrange
        var money1 = Money.Create(50m);
        var money2 = Money.Create(30m);

        // Act
        var result = money1.Add(money2);

        // Assert
        Assert.Equal(80m, result.Amount);
        Assert.Equal(50m, money1.Amount); // Original unchanged
    }

    [Fact]
    public void Money_Add_WithDifferentCurrency_ShouldThrowException()
    {
        // Arrange
        var usd = Money.Create(50m, "USD");
        var eur = Money.Create(30m, "EUR");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
    }

    [Fact]
    public void Money_Subtract_ShouldReturnNewMoney()
    {
        // Arrange
        var money1 = Money.Create(50m);
        var money2 = Money.Create(30m);

        // Act
        var result = money1.Subtract(money2);

        // Assert
        Assert.Equal(20m, result.Amount);
    }

    [Fact]
    public void Money_Multiply_ShouldReturnNewMoney()
    {
        // Arrange
        var money = Money.Create(10m);

        // Act
        var result = money.Multiply(3);

        // Assert
        Assert.Equal(30m, result.Amount);
    }

    #endregion

    #region ProductStatus Tests

    [Fact]
    public void ProductStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ProductStatus.Active);
        Assert.Equal(1, (int)ProductStatus.Inactive);
        Assert.Equal(2, (int)ProductStatus.Deleted);
    }

    [Fact]
    public void ProductStatus_CanBeParsedFromString()
    {
        // Act
        var active = Enum.Parse<ProductStatus>("Active");
        var inactive = Enum.Parse<ProductStatus>("Inactive");
        var deleted = Enum.Parse<ProductStatus>("Deleted");

        // Assert
        Assert.Equal(ProductStatus.Active, active);
        Assert.Equal(ProductStatus.Inactive, inactive);
        Assert.Equal(ProductStatus.Deleted, deleted);
    }

    #endregion
}
