using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Friends;

public class GroupMembershipDto
{
    public Guid Id { get; set; }
    public Guid ActivityGroupId { get; set; }
    public ProfileSummaryDto Account { get; set; } = null!;
    public GroupMemberRole Role { get; set; }
    public GroupMemberStatus Status { get; set; }
}
