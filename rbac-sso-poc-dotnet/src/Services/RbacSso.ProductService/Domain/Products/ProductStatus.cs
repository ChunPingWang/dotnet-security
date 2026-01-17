namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Represents the lifecycle status of a product.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is active and visible.
    /// </summary>
    Active,

    /// <summary>
    /// Product is temporarily inactive.
    /// Can be reactivated.
    /// </summary>
    Inactive,

    /// <summary>
    /// Product has been deleted (soft delete).
    /// This is a terminal state.
    /// </summary>
    Deleted
}
