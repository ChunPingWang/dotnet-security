using System.Text.RegularExpressions;
using RbacSso.Common.Exceptions;

namespace RbacSso.ProductService.Domain.Products;

/// <summary>
/// Value object representing a Product's code.
/// Format: "P" followed by 6 digits (e.g., P000001, P123456).
/// </summary>
public record ProductCode
{
    private static readonly Regex Pattern = new(@"^P\d{6}$", RegexOptions.Compiled);
    private static readonly Random Random = new();

    public string Value { get; }

    private ProductCode(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Generates a new random product code.
    /// </summary>
    public static ProductCode Generate()
    {
        var number = Random.Next(0, 999999);
        return new ProductCode($"P{number:D6}");
    }

    /// <summary>
    /// Creates a ProductCode from an existing string value.
    /// Throws if the format is invalid.
    /// </summary>
    public static ProductCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleException("PRD-V00001", "Product code cannot be empty");
        }

        if (!Pattern.IsMatch(value))
        {
            throw new BusinessRuleException("PRD-V00001",
                "Invalid product code format. Must be 'P' followed by 6 digits (e.g., P000001)");
        }

        return new ProductCode(value);
    }

    public override string ToString() => Value;
}
