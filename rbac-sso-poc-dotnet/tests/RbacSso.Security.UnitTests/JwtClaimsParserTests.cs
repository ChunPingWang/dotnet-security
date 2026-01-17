using FluentAssertions;
using RbacSso.Security.Authentication;
using RbacSso.Security.Authorization;
using System.Security.Claims;
using Xunit;

namespace RbacSso.Security.UnitTests;

/// <summary>
/// Unit tests for JWT claims parsing from Keycloak tokens.
/// </summary>
[Trait("Category", "Unit")]
public class JwtClaimsParserTests
{
    [Fact]
    public void GetUsername_WithPreferredUsername_ReturnsUsername()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(JwtClaimsPrincipalParser.PreferredUsernameClaimType, "admin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var username = JwtClaimsPrincipalParser.GetUsername(principal);

        // Assert
        username.Should().Be("admin");
    }

    [Fact]
    public void GetUsername_WithNameClaim_ReturnsName()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "user1")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var username = JwtClaimsPrincipalParser.GetUsername(principal);

        // Assert
        username.Should().Be("user1");
    }

    [Fact]
    public void GetUsername_WithSubClaim_ReturnsSub()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-uuid-123")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var username = JwtClaimsPrincipalParser.GetUsername(principal);

        // Assert
        username.Should().Be("user-uuid-123");
    }

    [Fact]
    public void GetUsername_WithNoClaims_ReturnsEmpty()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var username = JwtClaimsPrincipalParser.GetUsername(principal);

        // Assert
        username.Should().BeEmpty();
    }

    [Fact]
    public void GetTenantId_WithTenantIdClaim_ReturnsTenantId()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(JwtClaimsPrincipalParser.TenantIdClaimType, "tenant-a")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var tenantId = JwtClaimsPrincipalParser.GetTenantId(principal);

        // Assert
        tenantId.Should().Be("tenant-a");
    }

    [Fact]
    public void GetTenantId_WithNoTenantClaim_ReturnsEmpty()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var tenantId = JwtClaimsPrincipalParser.GetTenantId(principal);

        // Assert
        tenantId.Should().BeEmpty();
    }

    [Fact]
    public void GetRoles_WithStandardRoleClaims_ReturnsRoles()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, Roles.Admin),
            new Claim(ClaimTypes.Role, Roles.TenantAdmin)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var roles = JwtClaimsPrincipalParser.GetRoles(principal).ToList();

        // Assert
        roles.Should().Contain(Roles.Admin);
        roles.Should().Contain(Roles.TenantAdmin);
    }

    [Fact]
    public void GetRoles_WithAltRoleClaims_ReturnsRoles()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("role", Roles.User),
            new Claim("role", Roles.Viewer)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var roles = JwtClaimsPrincipalParser.GetRoles(principal).ToList();

        // Assert
        roles.Should().Contain(Roles.User);
        roles.Should().Contain(Roles.Viewer);
    }

    [Fact]
    public void HasRole_WhenRoleExists_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, Roles.Admin)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var hasRole = JwtClaimsPrincipalParser.HasRole(principal, Roles.Admin);

        // Assert
        hasRole.Should().BeTrue();
    }

    [Fact]
    public void HasRole_WhenRoleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, Roles.User)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var hasRole = JwtClaimsPrincipalParser.HasRole(principal, Roles.Admin);

        // Assert
        hasRole.Should().BeFalse();
    }

    [Fact]
    public void HasRole_IsCaseInsensitive()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "admin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var hasRole = JwtClaimsPrincipalParser.HasRole(principal, "ADMIN");

        // Assert
        hasRole.Should().BeTrue();
    }

    [Fact]
    public void GetEmail_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(JwtClaimsPrincipalParser.EmailClaimType, "admin@example.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var email = JwtClaimsPrincipalParser.GetEmail(principal);

        // Assert
        email.Should().Be("admin@example.com");
    }

    [Fact]
    public void GetSubject_WithSubClaim_ReturnsSubject()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-uuid-456")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        // Act
        var subject = JwtClaimsPrincipalParser.GetSubject(principal);

        // Assert
        subject.Should().Be("user-uuid-456");
    }
}
