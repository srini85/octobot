using Microsoft.Extensions.DependencyInjection;
using OctoBot.Core.Entities;
using OctoBot.LLM.Abstractions;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Agent;

public class AgentFactory : IAgentFactory
{
    private readonly ILLMProviderRegistry _llmRegistry;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IServiceScopeFactory _scopeFactory;

    public AgentFactory(
        ILLMProviderRegistry llmRegistry,
        IPluginRegistry pluginRegistry,
        IServiceScopeFactory scopeFactory)
    {
        _llmRegistry = llmRegistry;
        _pluginRegistry = pluginRegistry;
        _scopeFactory = scopeFactory;
    }

    public async Task<IOctoBotAgent> CreateAgentAsync(BotInstance botInstance, CancellationToken ct = default)
    {
        var agent = new OctoBotAgent(botInstance, _llmRegistry, _pluginRegistry, _scopeFactory);
        await agent.InitializeAsync(ct);
        return agent;
    }
}
