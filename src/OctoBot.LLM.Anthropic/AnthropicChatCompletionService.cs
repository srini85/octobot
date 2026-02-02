using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace OctoBot.LLM.Anthropic;

public class AnthropicChatCompletionService : IChatCompletionService
{
    private readonly string _modelId;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
    {
        ["ModelId"] = _modelId,
        ["Provider"] = "Anthropic"
    };

    public AnthropicChatCompletionService(string modelId, string apiKey, HttpClient? httpClient = null)
    {
        _modelId = modelId;
        _apiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var (systemPrompt, messages) = ConvertChatHistory(chatHistory);
        var request = CreateRequest(systemPrompt, messages, false);

        var response = await SendRequestAsync(request, cancellationToken);
        var content = ExtractContent(response);

        return [new ChatMessageContent(AuthorRole.Assistant, content)];
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (systemPrompt, messages) = ConvertChatHistory(chatHistory);
        var request = CreateRequest(systemPrompt, messages, true);

        await foreach (var chunk in StreamRequestAsync(request, cancellationToken))
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, chunk);
        }
    }

    private (string? systemPrompt, List<AnthropicMessage> messages) ConvertChatHistory(ChatHistory chatHistory)
    {
        string? systemPrompt = null;
        var messages = new List<AnthropicMessage>();

        foreach (var message in chatHistory)
        {
            if (message.Role == AuthorRole.System)
            {
                systemPrompt = message.Content;
            }
            else
            {
                var role = message.Role == AuthorRole.User ? "user" : "assistant";
                messages.Add(new AnthropicMessage(role, message.Content ?? ""));
            }
        }

        return (systemPrompt, messages);
    }

    private AnthropicRequest CreateRequest(string? systemPrompt, List<AnthropicMessage> messages, bool stream)
    {
        return new AnthropicRequest
        {
            Model = _modelId,
            MaxTokens = 4096,
            System = systemPrompt,
            Messages = messages,
            Stream = stream
        };
    }

    private async Task<AnthropicResponse> SendRequestAsync(AnthropicRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response from Anthropic API");
    }

    private async IAsyncEnumerable<string> StreamRequestAsync(AnthropicRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null && !cancellationToken.IsCancellationRequested)
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") break;

            var eventData = JsonSerializer.Deserialize<AnthropicStreamEvent>(data);
            if (eventData?.Type == "content_block_delta" && eventData.Delta?.Text != null)
            {
                yield return eventData.Delta.Text;
            }
        }
    }

    private static string ExtractContent(AnthropicResponse response)
    {
        var sb = new StringBuilder();
        foreach (var content in response.Content)
        {
            if (content.Type == "text")
            {
                sb.Append(content.Text);
            }
        }
        return sb.ToString();
    }
}

internal class AnthropicRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; } = 4096;

    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? System { get; init; }

    [JsonPropertyName("messages")]
    public required List<AnthropicMessage> Messages { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }
}

internal record AnthropicMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

internal class AnthropicResponse
{
    [JsonPropertyName("content")]
    public List<AnthropicContent> Content { get; init; } = [];
}

internal class AnthropicContent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

internal class AnthropicStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("delta")]
    public AnthropicDelta? Delta { get; init; }
}

internal class AnthropicDelta
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
