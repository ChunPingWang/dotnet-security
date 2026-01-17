namespace RbacSso.Tenant;

/// <summary>
/// Provides access to the current tenant context.
/// The tenant is resolved from JWT claims during request processing.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant's unique identifier.
    /// Extracted from the JWT 'tenant_id' claim.
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// Whether the current user is a system administrator who can access all tenants.
    /// </summary>
    bool IsSystemAdmin { get; }
}

/// <summary>
/// Interface for entities that belong to a specific tenant.
/// All tenant-scoped entities should implement this interface.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The tenant that owns this entity.
    /// </summary>
    string TenantId { get; }
}
