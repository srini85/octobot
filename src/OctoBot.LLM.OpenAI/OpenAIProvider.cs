using Microsoft.SemanticKernel;
using OctoBot.LLM.Abstractions;

namespace OctoBot.LLM.OpenAI;

public class OpenAIProvider : ILLMProvider
{
    public string Name => "openai";
    public string DisplayName => "OpenAI";
    public bool SupportsStreaming => true;
    public bool SupportsFunctionCalling => true;

    public IReadOnlyList<string> SupportedModels => new[]
    {
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo",
        "gpt-4",
        "gpt-3.5-turbo",
        "o1",
        "o1-mini",
        "o1-preview"
    };

    public Task<Kernel> CreateKernelAsync(LLMConfiguration config, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new ArgumentException("API key is required for OpenAI provider");
        }

        var builder = Kernel.CreateBuilder();

        if (!string.IsNullOrEmpty(config.Endpoint))
        {
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: config.ModelId,
                endpoint: config.Endpoint,
                apiKey: config.ApiKey
            );
        }
        else
        {
            builder.AddOpenAIChatCompletion(
                modelId: config.ModelId,
                apiKey: config.ApiKey
            );
        }

        return Task.FromResult(builder.Build());
    }

    public async Task<bool> ValidateConfigurationAsync(LLMConfiguration config, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            return false;
        }

        try
        {
            var kernel = await CreateKernelAsync(config, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
