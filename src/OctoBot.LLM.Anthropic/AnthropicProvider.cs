using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OctoBot.LLM.Abstractions;

namespace OctoBot.LLM.Anthropic;

public class AnthropicProvider : ILLMProvider
{
    public string Name => "anthropic";
    public string DisplayName => "Anthropic Claude";
    public bool SupportsStreaming => true;
    public bool SupportsFunctionCalling => true;

    public IReadOnlyList<string> SupportedModels => new[]
    {
        "claude-sonnet-4-20250514",
        "claude-opus-4-20250514",
        "claude-3-5-sonnet-20241022",
        "claude-3-5-haiku-20241022",
        "claude-3-opus-20240229",
        "claude-3-sonnet-20240229",
        "claude-3-haiku-20240307"
    };

    public Task<Kernel> CreateKernelAsync(LLMConfiguration config, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new ArgumentException("API key is required for Anthropic provider");
        }

        var builder = Kernel.CreateBuilder();
        var chatService = new AnthropicChatCompletionService(config.ModelId, config.ApiKey);
        builder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(chatService);

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
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var response = await httpClient.GetAsync("https://api.anthropic.com/v1/models", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
