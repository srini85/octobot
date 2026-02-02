namespace OctoBot.Plugins.Abstractions;

public interface IPluginLoader
{
    Task<IReadOnlyList<IPlugin>> LoadPluginsAsync(string pluginsDirectory, CancellationToken ct = default);
    Task<IPlugin?> LoadPluginAsync(string assemblyPath, CancellationToken ct = default);
    void UnloadPlugin(string pluginId);
}
