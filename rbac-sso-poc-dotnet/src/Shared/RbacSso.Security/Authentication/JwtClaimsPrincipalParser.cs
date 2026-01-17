using System.Security.Claims;

namespace RbacSso.Security.Authentication;

/// <summary>
/// Parses JWT claims into a structured user context.
/// Keycloak JWT tokens contain roles, tenant_id, and other custom claims.
/// </summary>
public static class JwtClaimsPrincipalParser
{
    /// <summary>
    /// Claim type for tenant ID in Keycloak tokens.
    /// </summary>
    public const string TenantIdClaimType = "tenant_id";

    /// <summary>
    /// Claim type for preferred username in Keycloak tokens.
    /// </summary>
    public const string PreferredUsernameClaimType = "preferred_username";

    /// <summary>
    /// Claim type for email in Keycloak tokens.
    /// </summary>
    public const string EmailClaimType = "email";

    /// <summary>
    /// Claim type for realm roles in Keycloak tokens.
    /// </summary>
    public const string RealmAccessClaimType = "realm_access";

    /// <summary>
    /// Extracts the username from the claims principal.
    /// </summary>
    public static string GetUsername(ClaimsPrincipal principal)
    {
        return principal.FindFirst(PreferredUsernameClaimType)?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? string.Empty;
    }

    /// <summary>
    /// Extracts the email from the claims principal.
    /// </summary>
    public static string? GetEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirst(EmailClaimType)?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Extracts the tenant ID from the claims principal.
    /// </summary>
    public static string GetTenantId(ClaimsPrincipal principal)
    {
        return principal.FindFirst(TenantIdClaimType)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Extracts all roles from the claims principal.
    /// Handles both standard role claims and Keycloak's realm_access structure.
    /// </summary>
    public static IEnumerable<string> GetRoles(ClaimsPrincipal principal)
    {
        // Standard role claims
        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);

        // Also check for "role" claim type (some JWT configurations use this)
        var altRoleClaims = principal.FindAll("role").Select(c => c.Value);

        return roleClaims.Concat(altRoleClaims).Distinct();
    }

    /// <summary>
    /// Checks if the principal has a specific role.
    /// </summary>
    public static bool HasRole(ClaimsPrincipal principal, string role)
    {
        return GetRoles(principal).Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the subject (user ID) from the claims principal.
    /// </summary>
    public static string GetSubject(ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;
    }
}
