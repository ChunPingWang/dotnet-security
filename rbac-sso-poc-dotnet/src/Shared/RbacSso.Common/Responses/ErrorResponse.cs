namespace RbacSso.Common.Responses;

/// <summary>
/// Standard error response for all API errors.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Always false for error responses.
    /// </summary>
    public bool Success { get; init; } = false;

    /// <summary>
    /// Error code following the pattern {COMPONENT}-{CATEGORY}{SEQUENCE}.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp of the error.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for request tracing.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Detailed error information (optional).
    /// </summary>
    public ErrorDetails? Error { get; init; }

    /// <summary>
    /// Creates a bad request error response.
    /// </summary>
    public static ErrorResponse BadRequest(string code, string message, string requestId, IEnumerable<FieldError>? fieldErrors = null)
    {
        return new ErrorResponse
        {
            Code = code,
            Message = message,
            RequestId = requestId,
            Error = new ErrorDetails
            {
                Code = code,
                Message = message,
                Details = fieldErrors?.ToList()
            }
        };
    }

    /// <summary>
    /// Creates an unauthorized error response.
    /// </summary>
    public static ErrorResponse Unauthorized(string requestId, string message = "Authentication required")
    {
        return new ErrorResponse
        {
            Code = "STD-S00401",
            Message = message,
            RequestId = requestId,
            Error = new ErrorDetails
            {
                Code = "STD-S00401",
                Message = message
            }
        };
    }

    /// <summary>
    /// Creates a forbidden error response.
    /// </summary>
    public static ErrorResponse Forbidden(string requestId, string message = "Access denied")
    {
        return new ErrorResponse
        {
            Code = "STD-S00403",
            Message = message,
            RequestId = requestId,
            Error = new ErrorDetails
            {
                Code = "STD-S00403",
                Message = message
            }
        };
    }

    /// <summary>
    /// Creates a not found error response.
    /// </summary>
    public static ErrorResponse NotFound(string code, string requestId, string message = "Resource not found")
    {
        return new ErrorResponse
        {
            Code = code,
            Message = message,
            RequestId = requestId,
            Error = new ErrorDetails
            {
                Code = code,
                Message = message
            }
        };
    }

    /// <summary>
    /// Creates an internal server error response.
    /// </summary>
    public static ErrorResponse InternalError(string requestId, string message = "An unexpected error occurred")
    {
        return new ErrorResponse
        {
            Code = "STD-S00500",
            Message = message,
            RequestId = requestId,
            Error = new ErrorDetails
            {
                Code = "STD-S00500",
                Message = message
            }
        };
    }
}

/// <summary>
/// Detailed error information.
/// </summary>
public record ErrorDetails
{
    /// <summary>
    /// Error code.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// List of field-level validation errors.
    /// </summary>
    public List<FieldError>? Details { get; init; }
}

/// <summary>
/// Field-level validation error.
/// </summary>
public record FieldError
{
    /// <summary>
    /// The name of the field with the error.
    /// </summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// The error message for the field.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
