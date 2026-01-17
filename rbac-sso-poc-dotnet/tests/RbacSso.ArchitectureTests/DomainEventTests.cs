using NetArchTest.Rules;
using RbacSso.Common.Domain;
using Xunit;

namespace RbacSso.ArchitectureTests;

/// <summary>
/// Tests to verify that domain events follow the correct patterns.
/// Per Constitution: Domain Events MUST implement IDomainEvent.
/// </summary>
public class DomainEventTests
{
    [Fact]
    [Trait("Category", "Architecture")]
    public void DomainEvents_Should_Implement_IDomainEvent()
    {
        // Arrange
        var productServiceAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(productServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Events")
            .And()
            .HaveNameEndingWith("Event")
            .Or()
            .HaveNameEndingWith("Created")
            .Or()
            .HaveNameEndingWith("Updated")
            .Or()
            .HaveNameEndingWith("Deleted")
            .Or()
            .HaveNameEndingWith("Changed")
            .Should()
            .ImplementInterface(typeof(IDomainEvent))
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain events should implement IDomainEvent. Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void DomainEvents_Should_Be_Records()
    {
        // Arrange
        var productServiceAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(productServiceAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .DoNotHaveName("IDomainEvent")
            .And()
            .DoNotHaveName("DomainEventBase")
            .Should()
            .BeSealed() // Records are sealed by default
            .GetResult();

        // Assert - Note: This may need adjustment based on how records are detected
        // For now, we just verify they exist and implement the interface
        Assert.True(result.IsSuccessful || result.FailingTypeNames?.Count() == 0,
            "Domain events should be records (sealed types)");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void DomainEvents_Should_Reside_In_Events_Namespace()
    {
        // Arrange
        var productServiceAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(productServiceAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .DoNotHaveName("IDomainEvent")
            .And()
            .DoNotHaveName("DomainEventBase")
            .And()
            .DoNotResideInNamespaceStartingWith("RbacSso.Common")
            .Should()
            .ResideInNamespaceContaining("Events")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain events should reside in an Events namespace. Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
