using Bloomdo.Shared.DTOs.Auth;

namespace Bloomdo.Client.Core.Interfaces;

public interface IAuthApiService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AccountProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default);
}
