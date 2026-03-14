namespace Bloomdo.Shared.DTOs.Activities;

public class UpdateActivityGroupRequest
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
