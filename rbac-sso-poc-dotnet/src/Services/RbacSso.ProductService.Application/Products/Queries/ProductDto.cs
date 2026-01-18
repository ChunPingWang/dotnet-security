namespace RbacSso.ProductService.Application.Products.Queries;

/// <summary>
/// Product DTO for query responses.
/// 產品資料傳輸物件
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string ProductCode,
    string Name,
    decimal Price,
    string Currency,
    string Category,
    string? Description,
    string Status,
    string TenantId,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string? UpdatedBy,
    DateTimeOffset? UpdatedAt
);

/// <summary>
/// Generic paged result wrapper.
/// 分頁結果包裝器
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int Size
)
{
    public int TotalPages => Size > 0 ? (int)Math.Ceiling(TotalCount / (double)Size) : 0;
    public bool HasPreviousPage => Page > 0;
    public bool HasNextPage => Page < TotalPages - 1;
}
