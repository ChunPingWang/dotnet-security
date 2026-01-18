namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Enumeration of product statuses.
/// 產品狀態列舉
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is active and visible.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Product is inactive (hidden from catalog).
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Product has been soft-deleted.
    /// </summary>
    Deleted = 2
}
