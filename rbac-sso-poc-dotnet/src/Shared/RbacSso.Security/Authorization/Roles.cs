namespace RbacSso.Security.Authorization;

/// <summary>
/// Constants for role names used in RBAC.
/// These map to Keycloak realm roles.
/// </summary>
public static class Roles
{
    /// <summary>
    /// System administrator with full access to all tenants and operations.
    /// Can perform any action across the entire system.
    /// </summary>
    public const string Admin = "ADMIN";

    /// <summary>
    /// Tenant administrator who can manage products and users within their tenant.
    /// Cannot delete products or access other tenants.
    /// </summary>
    public const string TenantAdmin = "TENANT_ADMIN";

    /// <summary>
    /// Regular user who can view products and place orders within their tenant.
    /// Cannot create, update, or delete products.
    /// </summary>
    public const string User = "USER";

    /// <summary>
    /// Read-only user who can only view product information.
    /// Cannot perform any write operations.
    /// </summary>
    public const string Viewer = "VIEWER";

    /// <summary>
    /// All roles that can read products.
    /// </summary>
    public static readonly string[] ProductReaders = { Admin, TenantAdmin, User, Viewer };

    /// <summary>
    /// Roles that can create and update products.
    /// </summary>
    public static readonly string[] ProductWriters = { Admin, TenantAdmin };

    /// <summary>
    /// Roles that can delete products.
    /// </summary>
    public static readonly string[] ProductDeleters = { Admin };

    /// <summary>
    /// Roles that can view audit logs.
    /// </summary>
    public static readonly string[] AuditReaders = { Admin, TenantAdmin };

    /// <summary>
    /// Roles that can manage users.
    /// </summary>
    public static readonly string[] UserManagers = { Admin };
}

/// <summary>
/// Authorization policy names for use with [Authorize] attribute.
/// </summary>
public static class Policies
{
    /// <summary>
    /// Policy for reading products (all authenticated users).
    /// </summary>
    public const string ProductRead = "ProductRead";

    /// <summary>
    /// Policy for creating/updating products (ADMIN, TENANT_ADMIN).
    /// </summary>
    public const string ProductWrite = "ProductWrite";

    /// <summary>
    /// Policy for deleting products (ADMIN only).
    /// </summary>
    public const string ProductDelete = "ProductDelete";

    /// <summary>
    /// Policy for reading audit logs (ADMIN, TENANT_ADMIN).
    /// </summary>
    public const string AuditRead = "AuditRead";

    /// <summary>
    /// Policy for managing users (ADMIN only).
    /// </summary>
    public const string UserManage = "UserManage";
}
