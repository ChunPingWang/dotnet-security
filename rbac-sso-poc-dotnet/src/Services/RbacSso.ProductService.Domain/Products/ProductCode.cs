using System.Text.RegularExpressions;
using RbacSso.ProductService.Domain.Common;

namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Value object representing a unique product code.
/// 產品代碼值物件
/// </summary>
public sealed partial class ProductCode : ValueObject
{
    private static readonly Regex CodePattern = MyRegex();

    public string Value { get; }

    private ProductCode(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a ProductCode from an existing code string.
    /// </summary>
    public static ProductCode Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("PRD-V00001", "Product code cannot be empty");

        if (!CodePattern.IsMatch(code))
            throw new ValidationException("PRD-V00002",
                "Product code must follow pattern PRD-XXXXXXXX (8 alphanumeric characters)");

        return new ProductCode(code.ToUpperInvariant());
    }

    /// <summary>
    /// Generates a new unique product code.
    /// </summary>
    public static ProductCode Generate()
    {
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return new ProductCode($"PRD-{random}");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(ProductCode code) => code.Value;

    [GeneratedRegex(@"^PRD-[A-Z0-9]{8}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
