namespace RbacSso.Tenant;

/// <summary>
/// Interface for entities that belong to a specific tenant.
/// Entities implementing this interface will have automatic tenant filtering applied.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The tenant identifier this entity belongs to.
    /// </summary>
    string TenantId { get; }
}
