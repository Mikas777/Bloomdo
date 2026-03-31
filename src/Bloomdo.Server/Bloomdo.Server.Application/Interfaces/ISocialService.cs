using Bloomdo.Shared.DTOs.Friends;

namespace Bloomdo.Server.Application.Interfaces;

public interface ISocialService
{
    Task<List<ProfileSummaryDto>> SearchUsersAsync(Guid currentAccountId, string query, CancellationToken ct = default);
    Task<List<FriendshipDto>> GetFriendsAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> SendFriendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken ct = default);
    Task<bool> RespondToFriendRequestAsync(Guid addresseeId, Guid friendshipId, bool accept, CancellationToken ct = default);
    Task<bool> RemoveFriendAsync(Guid accountId, Guid friendId, CancellationToken ct = default);
}
