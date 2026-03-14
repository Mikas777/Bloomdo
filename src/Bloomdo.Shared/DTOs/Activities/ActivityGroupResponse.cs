namespace Bloomdo.Shared.DTOs.Activities;

public class ActivityGroupResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<ActivityItemResponse> Items { get; set; } = [];
}
