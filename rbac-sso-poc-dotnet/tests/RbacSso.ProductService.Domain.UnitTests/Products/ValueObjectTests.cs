using FluentAssertions;
using RbacSso.ProductService.Domain.Common;
using RbacSso.ProductService.Domain.Products;
using Xunit;

namespace RbacSso.ProductService.Domain.UnitTests.Products;

public class ValueObjectTests
{
    #region ProductId Tests

    [Fact]
    public void ProductId_Create_ShouldGenerateUniqueId()
    {
        var id1 = ProductId.Create();
        var id2 = ProductId.Create();

        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ProductId_From_ShouldCreateFromGuid()
    {
        var guid = Guid.NewGuid();
        var id = ProductId.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void ProductId_Equality_ShouldBeValueBased()
    {
        var guid = Guid.NewGuid();
        var id1 = ProductId.From(guid);
        var id2 = ProductId.From(guid);

        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void ProductId_ImplicitConversion_ShouldWork()
    {
        var id = ProductId.Create();
        Guid guid = id;

        guid.Should().Be(id.Value);
    }

    #endregion

    #region ProductCode Tests

    [Fact]
    public void ProductCode_Generate_ShouldCreateValidCode()
    {
        var code = ProductCode.Generate();

        code.Value.Should().StartWith("PRD-");
        code.Value.Should().HaveLength(12); // PRD- + 8 chars
    }

    [Fact]
    public void ProductCode_Create_WithValidCode_ShouldSucceed()
    {
        var code = ProductCode.Create("PRD-ABC12345");

        code.Value.Should().Be("PRD-ABC12345");
    }

    [Fact]
    public void ProductCode_Create_WithEmpty_ShouldThrowValidationException()
    {
        var act = () => ProductCode.Create("");

        act.Should().Throw<ValidationException>()
            .Where(e => e.Code == "PRD-V00001");
    }

    [Fact]
    public void ProductCode_Create_WithInvalidFormat_ShouldThrowValidationException()
    {
        var act = () => ProductCode.Create("INVALID");

        act.Should().Throw<ValidationException>()
            .Where(e => e.Code == "PRD-V00002");
    }

    [Fact]
    public void ProductCode_Equality_ShouldBeValueBased()
    {
        var code1 = ProductCode.Create("PRD-TEST1234");
        var code2 = ProductCode.Create("PRD-TEST1234");

        code1.Should().Be(code2);
    }

    [Fact]
    public void ProductCode_ImplicitConversion_ShouldWork()
    {
        var code = ProductCode.Create("PRD-TEST1234");
        string str = code;

        str.Should().Be("PRD-TEST1234");
    }

    #endregion

    #region Money Tests

    [Fact]
    public void Money_Create_WithValidAmount_ShouldSucceed()
    {
        var money = Money.Create(99.99m);

        money.Amount.Should().Be(99.99m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Create_WithCurrency_ShouldSucceed()
    {
        var money = Money.Create(100m, "EUR");

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Money_Create_WithNegativeAmount_ShouldThrowValidationException()
    {
        var act = () => Money.Create(-10m);

        act.Should().Throw<ValidationException>()
            .Where(e => e.Code == "PRD-V00003");
    }

    [Fact]
    public void Money_Create_ShouldRoundToTwoDecimals()
    {
        var money = Money.Create(99.999m);

        money.Amount.Should().Be(100.00m);
    }

    [Fact]
    public void Money_Zero_ShouldReturnZeroAmount()
    {
        var money = Money.Zero();

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Add_ShouldReturnNewMoney()
    {
        var money1 = Money.Create(50m);
        var money2 = Money.Create(30m);

        var result = money1.Add(money2);

        result.Amount.Should().Be(80m);
        money1.Amount.Should().Be(50m); // Original unchanged
    }

    [Fact]
    public void Money_Add_WithDifferentCurrency_ShouldThrowBusinessRuleException()
    {
        var usd = Money.Create(50m, "USD");
        var eur = Money.Create(30m, "EUR");

        var act = () => usd.Add(eur);

        act.Should().Throw<BusinessRuleException>()
            .Where(e => e.Code == "PRD-B00001");
    }

    [Fact]
    public void Money_Subtract_ShouldReturnNewMoney()
    {
        var money1 = Money.Create(50m);
        var money2 = Money.Create(30m);

        var result = money1.Subtract(money2);

        result.Amount.Should().Be(20m);
    }

    [Fact]
    public void Money_Multiply_ShouldReturnNewMoney()
    {
        var money = Money.Create(10m);

        var result = money.Multiply(3);

        result.Amount.Should().Be(30m);
    }

    [Fact]
    public void Money_Equality_ShouldBeValueBased()
    {
        var money1 = Money.Create(99.99m, "USD");
        var money2 = Money.Create(99.99m, "USD");
        var money3 = Money.Create(99.99m, "EUR");

        money1.Should().Be(money2);
        money1.Should().NotBe(money3);
    }

    [Fact]
    public void Money_ToString_ShouldReturnFormattedString()
    {
        var money = Money.Create(99.99m, "USD");

        money.ToString().Should().Be("99.99 USD");
    }

    #endregion

    #region ProductStatus Tests

    [Fact]
    public void ProductStatus_ShouldHaveExpectedValues()
    {
        ((int)ProductStatus.Active).Should().Be(0);
        ((int)ProductStatus.Inactive).Should().Be(1);
        ((int)ProductStatus.Deleted).Should().Be(2);
    }

    #endregion
}
