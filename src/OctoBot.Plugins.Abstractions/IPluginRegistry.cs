namespace OctoBot.Plugins.Abstractions;

public interface IPluginRegistry
{
    void Register(IPlugin plugin);
    void Unregister(string pluginId);
    IPlugin? GetPlugin(string pluginId);
    IReadOnlyList<IPlugin> GetAllPlugins();
    bool HasPlugin(string pluginId);
}
