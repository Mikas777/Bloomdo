using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Services;

public class SocialService(
    IRepository<Account> accountRepo,
    IRepository<Friendship> friendshipRepo,
    IDailyActivityService dailyActivityService) : ISocialService
{
    public async Task<List<ProfileSummaryDto>> SearchUsersAsync(Guid currentAccountId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var normalizedQuery = query.ToLower();
        var users = await accountRepo.FindAsync(a => 
            a.Id != currentAccountId && 
            ((a.Username != null && a.Username.ToLower().Contains(normalizedQuery)) || 
             (a.Email.ToLower().Contains(normalizedQuery)) ||
             ((a.FirstName + " " + a.LastName).ToLower().Contains(normalizedQuery))), ct);

        return users.Select(MapToProfileSummary).ToList();
    }

    public async Task<List<FriendshipDto>> GetFriendsAsync(Guid accountId, CancellationToken ct = default)
    {
        var friendships = await friendshipRepo.FindAsync(f => 
            (f.RequesterId == accountId || f.AddresseeId == accountId) && 
            f.Status != FriendshipStatus.Blocked, ct);

        var result = new List<FriendshipDto>();
        foreach (var f in friendships)
        {
            var isRequester = f.RequesterId == accountId;
            var friendId = isRequester ? f.AddresseeId : f.RequesterId;
            var friend = await accountRepo.GetByIdAsync(friendId, ct);
            
            if (friend == null) continue;

            result.Add(new FriendshipDto
            {
                Id = f.Id,
                Friend = MapToProfileSummary(friend),
                Status = f.Status,
                IsIncomingRequest = !isRequester && f.Status == FriendshipStatus.Pending,
                CreatedAt = f.CreatedAt
            });
        }

        return result.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public async Task<bool> SendFriendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken ct = default)
    {
        if (requesterId == addresseeId) return false;

        var existing = await friendshipRepo.FirstOrDefaultAsync(f => 
            (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
            (f.RequesterId == addresseeId && f.AddresseeId == requesterId), ct);

        if (existing != null) return false;

        var friendship = new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendshipStatus.Pending
        };

        await friendshipRepo.AddAsync(friendship, ct);
        return true;
    }

    public async Task<bool> RespondToFriendRequestAsync(Guid addresseeId, Guid friendshipId, bool accept, CancellationToken ct = default)
    {
        var friendship = await friendshipRepo.GetByIdAsync(friendshipId, ct);
        if (friendship == null || friendship.AddresseeId != addresseeId || friendship.Status != FriendshipStatus.Pending)
            return false;

        if (accept)
        {
            friendship.Status = FriendshipStatus.Accepted;
            await friendshipRepo.UpdateAsync(friendship, ct);
        }
        else
        {
            await friendshipRepo.DeleteAsync(friendship, ct);
        }

        return true;
    }

    public async Task<bool> RemoveFriendAsync(Guid accountId, Guid friendId, CancellationToken ct = default)
    {
        var friendship = await friendshipRepo.FirstOrDefaultAsync(f => 
            (f.RequesterId == accountId && f.AddresseeId == friendId) ||
            (f.RequesterId == friendId && f.AddresseeId == accountId), ct);

        if (friendship == null) return false;

        await friendshipRepo.DeleteAsync(friendship, ct);
        return true;
    }

    private static ProfileSummaryDto MapToProfileSummary(Account account) => new()
    {
        Id = account.Id,
        Username = account.Username ?? string.Empty,
        FirstName = account.FirstName,
        LastName = account.LastName,
        AvatarJson = account.AvatarJson
    };
}
