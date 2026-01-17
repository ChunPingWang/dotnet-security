namespace RbacSso.Common.Exceptions;

/// <summary>
/// Base exception for all domain-level exceptions.
/// Error codes follow the pattern: {COMPONENT}-{CATEGORY}{SEQUENCE}
/// Example: PRD-B00001 (Product Business Error 00001)
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// The error code following the pattern {COMPONENT}-{CATEGORY}{SEQUENCE}.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Creates a new domain exception with the specified error code and message.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., PRD-B00001).</param>
    /// <param name="message">The error message.</param>
    protected DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a new domain exception with the specified error code, message, and inner exception.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., PRD-B00001).</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    protected DomainException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// Uses error category 'B' for business errors.
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string errorCode, string message)
        : base(errorCode, message)
    {
    }

    public BusinessRuleException(string errorCode, string message, Exception innerException)
        : base(errorCode, message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// Uses error category 'V' for validation errors.
/// </summary>
public class ValidationException : DomainException
{
    /// <summary>
    /// The validation errors, keyed by field name.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string errorCode, string message)
        : base(errorCode, message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string errorCode, string message, IDictionary<string, string[]> errors)
        : base(errorCode, message)
    {
        Errors = errors;
    }
}

/// <summary>
/// Exception thrown when a requested entity is not found.
/// Uses error category 'B' for business errors.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public string EntityId { get; }

    public EntityNotFoundException(string errorCode, string entityType, string entityId)
        : base(errorCode, $"{entityType} with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
