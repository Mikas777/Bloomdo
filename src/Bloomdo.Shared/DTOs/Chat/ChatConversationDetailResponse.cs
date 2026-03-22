namespace Bloomdo.Shared.DTOs.Chat;

public sealed class ChatConversationDetailResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<ChatMessageResponse> Messages { get; init; } = [];
}
