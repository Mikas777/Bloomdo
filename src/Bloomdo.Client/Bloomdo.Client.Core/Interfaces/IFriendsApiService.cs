using Bloomdo.Shared.DTOs.Friends;

namespace Bloomdo.Client.Core.Interfaces;

public interface IFriendsApiService
{
    Task<List<ProfileSummaryDto>> SearchUsersAsync(string query);
    Task<List<FriendshipDto>> GetFriendsAsync();
    Task<bool> SendFriendRequestAsync(Guid friendId);
    Task<bool> RespondToRequestAsync(Guid friendshipId, bool accept);
    Task<bool> RemoveFriendAsync(Guid friendId);
}
