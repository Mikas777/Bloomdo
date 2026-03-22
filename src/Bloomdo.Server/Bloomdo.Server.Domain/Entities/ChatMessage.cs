namespace Bloomdo.Server.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }

    /// <summary>
    /// "user" or "assistant"
    /// </summary>
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;

    public ChatConversation Conversation { get; set; } = null!;
}
