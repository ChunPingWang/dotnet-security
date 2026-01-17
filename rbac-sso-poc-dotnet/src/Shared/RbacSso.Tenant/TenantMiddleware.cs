using Microsoft.AspNetCore.Http;
using RbacSso.Security.Authorization;

namespace RbacSso.Tenant;

/// <summary>
/// Middleware that extracts tenant information from the authenticated user's JWT claims
/// and makes it available via ITenantContext for the duration of the request.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContextAccessor tenantContextAccessor)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = context.User.FindFirst("tenant_id")?.Value ?? string.Empty;
            var isAdmin = context.User.IsInRole(Roles.Admin);

            tenantContextAccessor.SetTenant(tenantId, isAdmin);
        }

        await _next(context);
    }
}

/// <summary>
/// Scoped service that holds the tenant context for the current request.
/// </summary>
public class TenantContextAccessor : ITenantContext
{
    private string _tenantId = string.Empty;
    private bool _isSystemAdmin;

    public string TenantId => _tenantId;
    public bool IsSystemAdmin => _isSystemAdmin;

    internal void SetTenant(string tenantId, bool isSystemAdmin)
    {
        _tenantId = tenantId;
        _isSystemAdmin = isSystemAdmin;
    }
}
