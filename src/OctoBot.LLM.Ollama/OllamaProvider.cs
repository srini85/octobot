using Microsoft.Extensions.AI;
using OctoBot.LLM.Abstractions;

namespace OctoBot.LLM.Ollama;

public class OllamaProvider : ILLMProvider
{
    public string Name => "ollama";
    public string DisplayName => "Ollama (Local)";
    public bool SupportsStreaming => true;
    public bool SupportsFunctionCalling => true;

    public IReadOnlyList<string> SupportedModels => new[]
    {
        "llama3.2",
        "llama3.1",
        "llama3",
        "llama2",
        "mistral",
        "mixtral",
        "codellama",
        "phi3",
        "gemma2",
        "qwen2.5",
        "deepseek-coder-v2"
    };

    public Task<IChatClient> CreateChatClientAsync(LLMConfiguration config, CancellationToken ct = default)
    {
        var endpoint = config.Endpoint ?? "http://localhost:11434";

        IChatClient chatClient = new OllamaChatClient(new Uri(endpoint), config.ModelId);

        return Task.FromResult(chatClient);
    }

    public async Task<bool> ValidateConfigurationAsync(LLMConfiguration config, CancellationToken ct = default)
    {
        try
        {
            var endpoint = config.Endpoint ?? "http://localhost:11434";
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await httpClient.GetAsync($"{endpoint.TrimEnd('/')}/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
