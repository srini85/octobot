using Microsoft.Extensions.AI;

namespace OctoBot.Plugins.Abstractions;

public interface IPlugin
{
    PluginMetadata Metadata { get; }
    IEnumerable<AIFunction> GetFunctions();
    Task InitializeAsync(IServiceProvider services, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}
