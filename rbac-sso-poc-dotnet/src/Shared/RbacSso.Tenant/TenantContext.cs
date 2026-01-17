using Microsoft.AspNetCore.Http;
using RbacSso.Security.Authorization;

namespace RbacSso.Tenant;

/// <summary>
/// Implementation of ITenantContext that resolves tenant from JWT claims.
/// </summary>
public class TenantContext : ITenantContext
{
    private const string TenantClaimType = "tenant_id";
    private const string DefaultTenant = "system";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _tenantId;
    private bool? _isAdmin;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId
    {
        get
        {
            if (_tenantId is null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    _tenantId = DefaultTenant;
                }
                else
                {
                    var tenantClaim = httpContext.User.Claims
                        .FirstOrDefault(c => c.Type == TenantClaimType);
                    _tenantId = tenantClaim?.Value ?? DefaultTenant;
                }
            }

            return _tenantId;
        }
    }

    public bool IsAdmin
    {
        get
        {
            if (_isAdmin is null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                _isAdmin = httpContext?.User?.IsInRole(Roles.Admin) ?? false;
            }

            return _isAdmin.Value;
        }
    }
}
