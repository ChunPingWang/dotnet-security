using System.Security.Claims;
using RbacSso.ProductService.Application.Ports;

namespace RbacSso.ProductService.Api.Services;

/// <summary>
/// ASP.NET Core implementation of ICurrentUserService.
/// Extracts user info from JWT claims.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub")
        ?? string.Empty;

    public string Username => User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.FindFirstValue("preferred_username")
        ?? "anonymous";

    public string TenantId => User?.FindFirstValue("tenant_id") ?? "system";

    public IReadOnlyList<string> Roles
    {
        get
        {
            var roles = new List<string>();

            // Standard role claims
            roles.AddRange(User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? []);

            // Keycloak realm roles
            roles.AddRange(User?.FindAll("realm_access.roles").Select(c => c.Value) ?? []);

            return roles.Distinct().ToList().AsReadOnly();
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => Roles.Contains("ADMIN");
}
