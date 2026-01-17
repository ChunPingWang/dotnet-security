namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Value object representing a Product's unique identifier.
/// </summary>
public record ProductId
{
    public Guid Value { get; }

    private ProductId(Guid value)
    {
        Value = value;
    }

    public static ProductId Create() => new(Guid.NewGuid());

    public static ProductId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
