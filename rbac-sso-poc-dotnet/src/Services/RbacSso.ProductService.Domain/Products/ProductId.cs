using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Strongly-typed identifier for Product aggregate.
/// 產品聚合的強型別識別碼
/// </summary>
public sealed class ProductId : ValueObject
{
    public Guid Value { get; }

    private ProductId(Guid value)
    {
        Value = value;
    }

    public static ProductId Create() => new(Guid.NewGuid());
    public static ProductId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ProductId id) => id.Value;
}
