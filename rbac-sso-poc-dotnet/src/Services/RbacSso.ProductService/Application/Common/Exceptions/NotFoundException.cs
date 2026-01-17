namespace RbacSso.ProductService.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public NotFoundException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
