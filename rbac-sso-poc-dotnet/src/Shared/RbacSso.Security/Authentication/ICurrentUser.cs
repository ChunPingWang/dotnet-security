namespace RbacSso.Security.Authentication;

/// <summary>
/// Interface for accessing the current authenticated user's information.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The username of the current user.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// The email of the current user.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The unique subject identifier.
    /// </summary>
    string Subject { get; }

    /// <summary>
    /// The tenant ID of the current user.
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// The roles assigned to the current user.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Implementation of ICurrentUser that extracts user info from HttpContext.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private System.Security.Claims.ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    public string Username =>
        JwtClaimsPrincipalParser.GetUsername(Principal ?? new System.Security.Claims.ClaimsPrincipal());

    public string? Email =>
        JwtClaimsPrincipalParser.GetEmail(Principal ?? new System.Security.Claims.ClaimsPrincipal());

    public string Subject =>
        JwtClaimsPrincipalParser.GetSubject(Principal ?? new System.Security.Claims.ClaimsPrincipal());

    public string TenantId =>
        JwtClaimsPrincipalParser.GetTenantId(Principal ?? new System.Security.Claims.ClaimsPrincipal());

    public IEnumerable<string> Roles =>
        JwtClaimsPrincipalParser.GetRoles(Principal ?? new System.Security.Claims.ClaimsPrincipal());
}
