namespace OctoBot.Core.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public required MessageRole Role { get; set; }
    public required string Content { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }

    public Conversation? Conversation { get; set; }
}

public enum MessageRole
{
    User,
    Assistant,
    System,
    Tool
}
