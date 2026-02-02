namespace OctoBot.Core.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid BotInstanceId { get; set; }
    public required string ChannelId { get; set; }
    public required string UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }

    public BotInstance? BotInstance { get; set; }
    public ICollection<Message> Messages { get; set; } = [];
}
