using Microsoft.AspNetCore.Http;

namespace RbacSso.Tenant;

/// <summary>
/// Resolves tenant ID from JWT claims.
/// </summary>
public class ClaimTenantResolver
{
    private const string TenantClaimType = "tenant_id";
    private const string DefaultTenant = "system";

    /// <summary>
    /// Resolves the tenant ID from the current HTTP context.
    /// </summary>
    public string ResolveTenantId(HttpContext httpContext)
    {
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return DefaultTenant;
        }

        var tenantClaim = httpContext.User.Claims
            .FirstOrDefault(c => c.Type == TenantClaimType);

        return tenantClaim?.Value ?? DefaultTenant;
    }
}
