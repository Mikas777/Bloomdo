namespace Bloomdo.Shared.DTOs.Chat;

public sealed class SendMessageResponse
{
    public ChatMessageResponse UserMessage { get; init; } = null!;
    public ChatMessageResponse AssistantMessage { get; init; } = null!;
}
