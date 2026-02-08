namespace OctoBot.Plugins.Abstractions;

/// <summary>
/// Optional interface for plugins that accept per-bot configuration settings.
/// When a bot instance initializes its agent, plugins implementing this interface
/// will receive their stored settings from the PluginConfig entity.
/// </summary>
public interface IConfigurablePlugin
{
    void Configure(Dictionary<string, string> settings);
}
