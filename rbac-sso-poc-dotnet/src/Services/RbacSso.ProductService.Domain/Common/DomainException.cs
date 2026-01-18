namespace RbacSso.ProductService.Domain.Common;

/// <summary>
/// Base exception for domain rule violations.
/// 領域規則違反例外基底類別
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// Exception for business rule violations.
/// 商業規則違反例外
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string code, string message)
        : base(code, message)
    {
    }
}

/// <summary>
/// Exception for validation errors.
/// 驗證錯誤例外
/// </summary>
public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string code, string message, IDictionary<string, string[]>? errors = null)
        : base(code, message)
    {
        Errors = errors?.AsReadOnly() ?? new Dictionary<string, string[]>().AsReadOnly();
    }
}
