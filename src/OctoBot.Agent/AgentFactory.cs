using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.LLM.Abstractions;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Agent;

public class AgentFactory : IAgentFactory
{
    private readonly ILLMProviderRegistry _llmRegistry;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IConversationMemory _memory;

    public AgentFactory(
        ILLMProviderRegistry llmRegistry,
        IPluginRegistry pluginRegistry,
        IConversationMemory memory)
    {
        _llmRegistry = llmRegistry;
        _pluginRegistry = pluginRegistry;
        _memory = memory;
    }

    public async Task<IOctoBotAgent> CreateAgentAsync(BotInstance botInstance, CancellationToken ct = default)
    {
        var agent = new OctoBotAgent(botInstance, _llmRegistry, _pluginRegistry, _memory);
        await agent.InitializeAsync(ct);
        return agent;
    }
}
