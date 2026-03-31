namespace Bloomdo.Server.Domain.Entities;

public class ActivityGroup : BaseEntity
{
    public Guid AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = "📋";
    public string Color { get; set; } = "#7E57C2";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Account Account { get; set; } = null!;
    public ICollection<ActivityItem> Items { get; set; } = [];
    public ICollection<GroupMembership> Memberships { get; set; } = [];
}
