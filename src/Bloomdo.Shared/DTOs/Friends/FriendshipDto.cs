using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Friends;

public class FriendshipDto
{
    public Guid Id { get; set; }
    public ProfileSummaryDto Friend { get; set; } = null!;
    public FriendshipStatus Status { get; set; }
    public bool IsIncomingRequest { get; set; }
    public DateTime CreatedAt { get; set; }
}
