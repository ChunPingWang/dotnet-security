namespace RbacSso.ProductService.Application.Ports;

/// <summary>
/// Port interface for accessing current user information.
/// 當前使用者資訊 Port 介面
/// </summary>
public interface ICurrentUserService
{
    string UserId { get; }
    string Username { get; }
    string TenantId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
