namespace Bloomdo.Client.Core.Interfaces;

public interface ITokenStorage
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task ClearTokensAsync();
}
