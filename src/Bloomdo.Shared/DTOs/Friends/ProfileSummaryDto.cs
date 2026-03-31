namespace Bloomdo.Shared.DTOs.Friends;

public class ProfileSummaryDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarJson { get; set; }
}
