using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Domain.Entities;

public class GroupMembership : BaseEntity
{
    public Guid ActivityGroupId { get; set; }
    public Guid AccountId { get; set; }
    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
    public GroupMemberStatus Status { get; set; } = GroupMemberStatus.Pending;

    public ActivityGroup Group { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
