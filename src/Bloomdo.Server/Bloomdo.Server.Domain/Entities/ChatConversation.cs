namespace Bloomdo.Server.Domain.Entities;

public class ChatConversation : BaseEntity
{
    public Guid AccountId { get; set; }
    public string Title { get; set; } = "New Chat";

    public Account Account { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
