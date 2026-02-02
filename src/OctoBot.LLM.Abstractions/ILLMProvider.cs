using Microsoft.SemanticKernel;

namespace OctoBot.LLM.Abstractions;

public interface ILLMProvider
{
    string Name { get; }
    string DisplayName { get; }
    bool SupportsStreaming { get; }
    bool SupportsFunctionCalling { get; }
    IReadOnlyList<string> SupportedModels { get; }

    Task<Kernel> CreateKernelAsync(LLMConfiguration config, CancellationToken ct = default);
    Task<bool> ValidateConfigurationAsync(LLMConfiguration config, CancellationToken ct = default);
}
