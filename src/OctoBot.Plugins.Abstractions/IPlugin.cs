using Microsoft.SemanticKernel;

namespace OctoBot.Plugins.Abstractions;

public interface IPlugin
{
    PluginMetadata Metadata { get; }
    void RegisterFunctions(IKernelBuilder builder);
    Task InitializeAsync(IServiceProvider services, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}
