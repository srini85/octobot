using Microsoft.Extensions.DependencyInjection;

namespace OctoBot.Plugins.Abstractions;

/// <summary>
/// Optional interface for plugins that accept per-bot configuration settings.
/// When a bot instance initializes its agent, plugins implementing this interface
/// will receive their stored settings, bot instance ID, and a scope factory
/// for accessing scoped services like the database.
/// </summary>
public interface IConfigurablePlugin
{
    void Configure(Guid botInstanceId, Dictionary<string, string> settings, IServiceScopeFactory scopeFactory);
}
