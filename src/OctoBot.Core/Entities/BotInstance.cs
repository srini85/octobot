namespace OctoBot.Core.Entities;

public class BotInstance
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? SystemPrompt { get; set; }
    public Guid? DefaultLLMConfigId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public LLMConfig? DefaultLLMConfig { get; set; }
    public ICollection<Conversation> Conversations { get; set; } = [];
    public ICollection<ChannelConfig> ChannelConfigs { get; set; } = [];
    public ICollection<PluginConfig> PluginConfigs { get; set; } = [];
}
