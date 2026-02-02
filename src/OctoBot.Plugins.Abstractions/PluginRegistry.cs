using System.Collections.Concurrent;

namespace OctoBot.Plugins.Abstractions;

public class PluginRegistry : IPluginRegistry
{
    private readonly ConcurrentDictionary<string, IPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

    public PluginRegistry(IEnumerable<IPlugin> plugins)
    {
        foreach (var plugin in plugins)
        {
            Register(plugin);
        }
    }

    public void Register(IPlugin plugin)
    {
        _plugins.TryAdd(plugin.Metadata.Id, plugin);
    }

    public void Unregister(string pluginId)
    {
        _plugins.TryRemove(pluginId, out _);
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        _plugins.TryGetValue(pluginId, out var plugin);
        return plugin;
    }

    public IReadOnlyList<IPlugin> GetAllPlugins()
    {
        return _plugins.Values.ToList();
    }

    public bool HasPlugin(string pluginId)
    {
        return _plugins.ContainsKey(pluginId);
    }
}
