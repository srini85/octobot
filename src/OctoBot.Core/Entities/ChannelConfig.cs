namespace OctoBot.Core.Entities;

public class ChannelConfig
{
    public Guid Id { get; set; }
    public Guid BotInstanceId { get; set; }
    public required string ChannelType { get; set; }
    public bool IsEnabled { get; set; }
    public string? Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public BotInstance? BotInstance { get; set; }
}
