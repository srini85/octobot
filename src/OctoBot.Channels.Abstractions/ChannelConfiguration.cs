namespace OctoBot.Channels.Abstractions;

public class ChannelConfiguration
{
    public Guid BotInstanceId { get; init; }
    public required string ChannelType { get; init; }
    public Dictionary<string, string> Settings { get; init; } = [];
}
