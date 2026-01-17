namespace RbacSso.Common.Responses;

/// <summary>
/// Standard API response wrapper for all endpoints.
/// Provides a consistent response format across all services.
/// </summary>
/// <typeparam name="T">The type of the response data.</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    /// Whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response code (success code or error code).
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The response data (null if error).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Timestamp of the response.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for request tracing.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string requestId, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = "OK",
            Message = message,
            Data = data,
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates a successful response for creation operations.
    /// </summary>
    public static ApiResponse<T> Created(T data, string requestId, string message = "Created successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = "CREATED",
            Message = message,
            Data = data,
            RequestId = requestId
        };
    }
}

/// <summary>
/// Non-generic API response for operations without return data.
/// </summary>
public record ApiResponse
{
    public bool Success { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string RequestId { get; init; } = string.Empty;

    public static ApiResponse Ok(string requestId, string message = "Success")
    {
        return new ApiResponse
        {
            Success = true,
            Code = "OK",
            Message = message,
            RequestId = requestId
        };
    }

    public static ApiResponse NoContent(string requestId)
    {
        return new ApiResponse
        {
            Success = true,
            Code = "NO_CONTENT",
            Message = "Operation completed successfully",
            RequestId = requestId
        };
    }
}
