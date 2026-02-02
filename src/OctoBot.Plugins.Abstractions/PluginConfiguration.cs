namespace OctoBot.Plugins.Abstractions;

public class PluginConfiguration
{
    public required string PluginId { get; init; }
    public bool IsEnabled { get; init; }
    public Dictionary<string, string> Settings { get; init; } = [];
}
