namespace OctoBot.Core.Entities;

public class LLMConfig
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ProviderType { get; set; }
    public string? ModelId { get; set; }
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<BotInstance> BotInstances { get; set; } = [];
}
