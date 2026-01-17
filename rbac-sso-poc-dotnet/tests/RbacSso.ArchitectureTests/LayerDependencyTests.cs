using NetArchTest.Rules;
using Xunit;

namespace RbacSso.ArchitectureTests;

/// <summary>
/// Tests to verify that architectural layer dependencies are correctly enforced.
/// Per Hexagonal Architecture principles:
/// - Domain layer MUST NOT depend on Infrastructure or Application
/// - Application layer MUST NOT depend on Infrastructure
/// </summary>
public class LayerDependencyTests
{
    private const string DomainNamespace = "RbacSso.ProductService.Domain";
    private const string ApplicationNamespace = "RbacSso.ProductService.Application";
    private const string InfrastructureNamespace = "RbacSso.ProductService.Infrastructure";
    private const string ApiNamespace = "RbacSso.ProductService.Api";

    [Fact]
    [Trait("Category", "Architecture")]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        // Arrange
        var domainAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not depend on Infrastructure. Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Domain_Should_Not_Depend_On_Application()
    {
        // Arrange
        var domainAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not depend on Application. Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Domain_Should_Not_Have_EFCore_Dependencies()
    {
        // Arrange
        var domainAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not have EF Core dependencies. Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Domain_Should_Not_Have_AspNetCore_Dependencies()
    {
        // Arrange
        var domainAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not have ASP.NET Core dependencies. Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        // Arrange
        var applicationAssembly = typeof(ProductService.Domain.Products.Product).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application layer should not depend on Infrastructure. Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
