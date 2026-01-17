namespace RbacSso.Audit.Domain;

/// <summary>
/// The result of an audited operation.
/// </summary>
public enum AuditResult
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The operation failed.
    /// </summary>
    Failure
}
