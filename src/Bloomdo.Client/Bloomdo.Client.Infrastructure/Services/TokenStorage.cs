using Bloomdo.Client.Core.Interfaces;
using Microsoft.Maui.Storage;

namespace Bloomdo.Client.Infrastructure.Services;

public class TokenStorage : ITokenStorage
{
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(AccessTokenKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get access token: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get refresh token: {ex.Message}");
            return null;
        }
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save tokens: {ex.Message}");
            throw;
        }
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear tokens: {ex.Message}");
        }
    }
}
