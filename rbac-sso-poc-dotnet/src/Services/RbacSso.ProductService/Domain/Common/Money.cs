using RbacSso.Common.Exceptions;

namespace RbacSso.ProductService.Domain.Common;

/// <summary>
/// Value object representing a monetary amount.
/// Enforces that amounts must be positive.
/// </summary>
public record Money
{
    public decimal Amount { get; }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    /// <summary>
    /// Creates a Money value object.
    /// Throws if amount is not positive.
    /// </summary>
    public static Money Create(decimal amount)
    {
        if (amount <= 0)
        {
            throw new BusinessRuleException("PRD-V00002", "Price must be a positive value");
        }

        return new Money(amount);
    }

    public override string ToString() => Amount.ToString("N2");
}
