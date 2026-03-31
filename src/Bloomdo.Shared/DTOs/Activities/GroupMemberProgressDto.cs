using Bloomdo.Shared.DTOs.Friends;

namespace Bloomdo.Shared.DTOs.Activities;

public class GroupMemberProgressDto
{
    public ProfileSummaryDto Account { get; set; } = null!;
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
}
