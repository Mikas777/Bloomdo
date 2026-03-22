namespace Bloomdo.Shared.DTOs.Chat;

public sealed class ChatMessageResponse
{
    public Guid Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
