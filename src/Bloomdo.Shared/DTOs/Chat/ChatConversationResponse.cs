namespace Bloomdo.Shared.DTOs.Chat;

public sealed class ChatConversationResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public ChatMessageResponse? LastMessage { get; init; }
}
