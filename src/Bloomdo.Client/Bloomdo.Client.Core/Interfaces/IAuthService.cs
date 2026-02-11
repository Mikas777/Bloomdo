using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Core.Interfaces;

public interface IAccessTokenManager
{
    string? AuthToken { get; }
    bool IsAuthenticated { get; }
    AccountProfileResponse? CurrentUser { get; }
    UserRole CurrentRole { get; }
    IReadOnlyList<string> CurrentPermissions { get; }

    Task<bool> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string email, string password, string? firstName, string? lastName);
    Task<bool> RefreshTokenAsync();
    Task LogoutAsync();
    Task InitializeAsync();

    bool HasPermission(string permission);
    bool HasAnyPermission(params string[] permissions);
}
