namespace OctoBot.LLM.Abstractions;

public class LLMConfiguration
{
    public required string ProviderName { get; init; }
    public required string ModelId { get; init; }
    public string? ApiKey { get; init; }
    public string? Endpoint { get; init; }
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 4096;
    public Dictionary<string, object> AdditionalSettings { get; init; } = [];
}
