namespace RbacSso.ProductService.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessRuleException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
