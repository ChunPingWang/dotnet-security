using Microsoft.AspNetCore.Authorization;
using RbacSso.Security.Authentication;

namespace RbacSso.Security.Authorization;

/// <summary>
/// Authorization requirement that checks if user has any of the specified roles.
/// </summary>
public class RolesRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public RolesRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}

/// <summary>
/// Handler for role-based authorization requirements.
/// Checks if the authenticated user has any of the required roles.
/// </summary>
public class RbacAuthorizationHandler : AuthorizationHandler<RolesRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RolesRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var userRoles = JwtClaimsPrincipalParser.GetRoles(context.User);

        if (requirement.AllowedRoles.Any(role =>
            userRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for configuring authorization policies.
/// </summary>
public static class AuthorizationPolicyExtensions
{
    /// <summary>
    /// Adds RBAC authorization policies to the authorization options.
    /// </summary>
    public static void AddRbacPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Policies.ProductRead, policy =>
            policy.Requirements.Add(new RolesRequirement(Roles.ProductReaders)));

        options.AddPolicy(Policies.ProductWrite, policy =>
            policy.Requirements.Add(new RolesRequirement(Roles.ProductWriters)));

        options.AddPolicy(Policies.ProductDelete, policy =>
            policy.Requirements.Add(new RolesRequirement(Roles.ProductDeleters)));

        options.AddPolicy(Policies.AuditRead, policy =>
            policy.Requirements.Add(new RolesRequirement(Roles.AuditReaders)));

        options.AddPolicy(Policies.UserManage, policy =>
            policy.Requirements.Add(new RolesRequirement(Roles.UserManagers)));
    }
}
