using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Friends;

namespace Bloomdo.Client.Infrastructure.Services;

public class FriendsApiService(HttpClient httpClient) : IFriendsApiService
{
    public async Task<List<ProfileSummaryDto>> SearchUsersAsync(string query)
    {
        try
        {
            var url = $"{ApiRoutes.Social.Search}?query={Uri.EscapeDataString(query)}";
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ProfileSummaryDto>>() ?? [];

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SearchUsers failed: {ex.Message}");
            return [];
        }
    }

    public async Task<List<FriendshipDto>> GetFriendsAsync()
    {
        try
        {
            var response = await httpClient.GetAsync(ApiRoutes.Social.Friends);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<FriendshipDto>>() ?? [];

            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetFriends failed: {ex.Message}");
            return [];
        }
    }

    public async Task<bool> SendFriendRequestAsync(Guid friendId)
    {
        try
        {
            var url = $"{ApiRoutes.Social.Request}?addresseeId={friendId}";
            var response = await httpClient.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendFriendRequest failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RespondToRequestAsync(Guid friendshipId, bool accept)
    {
        try
        {
            var url = ApiRoutes.Social.Respond.Replace("{id}", friendshipId.ToString()) + $"?accept={accept}";
            var response = await httpClient.PutAsync(url, null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RespondToRequest failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveFriendAsync(Guid friendId)
    {
        try
        {
            var url = ApiRoutes.Social.Remove.Replace("{id}", friendId.ToString());
            var response = await httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RemoveFriend failed: {ex.Message}");
            return false;
        }
    }
}
