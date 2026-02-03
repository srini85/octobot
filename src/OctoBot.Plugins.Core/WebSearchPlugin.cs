using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Plugins.Core;

public class WebSearchPlugin : IPlugin
{
    private HttpClient? _httpClient;
    private string? _apiKey;
    private readonly WebSearchFunctions _functions;

    public WebSearchPlugin()
    {
        _functions = new WebSearchFunctions(this);
    }

    public PluginMetadata Metadata => new(
        Id: "websearch",
        Name: "Web Search",
        Description: "Provides web search capabilities",
        Version: "1.0.0",
        Author: "OctoBot",
        Settings: new[]
        {
            new PluginSettingDefinition(
                Key: "ApiKey",
                DisplayName: "API Key",
                Description: "The Brave Search API key",
                Type: PluginSettingType.Secret,
                IsRequired: true
            )
        }
    );

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(_functions.Search, name: "WebSearch_Search");
    }

    public Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        _httpClient = new HttpClient();
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }

    public void Configure(string apiKey)
    {
        _apiKey = apiKey;
    }

    internal async Task<string> SearchAsync(string query, int count = 5)
    {
        if (_httpClient == null || string.IsNullOrEmpty(_apiKey))
        {
            return "Web search is not configured. Please set the API key.";
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count={count}");
            request.Headers.Add("X-Subscription-Token", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BraveSearchResponse>();
            if (result?.Web?.Results == null || result.Web.Results.Count == 0)
            {
                return "No results found.";
            }

            var sb = new System.Text.StringBuilder();
            foreach (var item in result.Web.Results.Take(count))
            {
                sb.AppendLine($"**{item.Title}**");
                sb.AppendLine(item.Description);
                sb.AppendLine($"URL: {item.Url}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Search failed: {ex.Message}";
        }
    }
}

public class WebSearchFunctions
{
    private readonly WebSearchPlugin _plugin;

    public WebSearchFunctions(WebSearchPlugin plugin)
    {
        _plugin = plugin;
    }

    [Description("Searches the web for information")]
    public async Task<string> Search(
        [Description("The search query")] string query,
        [Description("Maximum number of results (default: 5)")] int maxResults = 5)
    {
        return await _plugin.SearchAsync(query, maxResults);
    }
}

internal class BraveSearchResponse
{
    [JsonPropertyName("web")]
    public BraveWebResults? Web { get; set; }
}

internal class BraveWebResults
{
    [JsonPropertyName("results")]
    public List<BraveSearchResult>? Results { get; set; }
}

internal class BraveSearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}
