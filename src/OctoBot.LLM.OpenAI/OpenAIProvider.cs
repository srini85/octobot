using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OctoBot.LLM.Abstractions;
using OpenAI;
using OpenAI.Chat;

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

    public Task<IChatClient> CreateChatClientAsync(LLMConfiguration config, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new ArgumentException("API key is required for OpenAI provider");
        }

        IChatClient chatClient;

        if (!string.IsNullOrEmpty(config.Endpoint))
        {
            // Azure OpenAI
            var azureClient = new AzureOpenAIClient(
                new Uri(config.Endpoint),
                new System.ClientModel.ApiKeyCredential(config.ApiKey));
            var azureChatClient = azureClient.GetChatClient(config.ModelId);
            chatClient = azureChatClient.AsIChatClient();
        }
        else
        {
            // OpenAI
            var openAIChatClient = new ChatClient(config.ModelId, new System.ClientModel.ApiKeyCredential(config.ApiKey));
            chatClient = openAIChatClient.AsIChatClient();
        }

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
            var chatClient = await CreateChatClientAsync(config, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
