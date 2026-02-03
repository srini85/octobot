using Anthropic;
using Microsoft.Extensions.AI;
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

    public Task<IChatClient> CreateChatClientAsync(LLMConfiguration config, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new ArgumentException("API key is required for Anthropic provider");
        }

        var anthropicClient = new AnthropicClient(new() { ApiKey = config.ApiKey });
        // The Anthropic SDK implements IChatClient - use explicit cast
        IChatClient chatClient = (IChatClient)anthropicClient;

        return Task.FromResult(chatClient);
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
