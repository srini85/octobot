using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;

namespace OctoBot.Agent;

public interface IAgentManager
{
    Task<IOctoBotAgent> GetOrCreateAgentAsync(Guid botInstanceId, CancellationToken ct = default);
    void RemoveAgent(Guid botInstanceId);
    bool HasAgent(Guid botInstanceId);
}

public class AgentManager : IAgentManager
{
    private readonly IAgentFactory _agentFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, IOctoBotAgent> _agents = new();

    public AgentManager(IAgentFactory agentFactory, IServiceScopeFactory scopeFactory)
    {
        _agentFactory = agentFactory;
        _scopeFactory = scopeFactory;
    }

    public async Task<IOctoBotAgent> GetOrCreateAgentAsync(Guid botInstanceId, CancellationToken ct = default)
    {
        if (_agents.TryGetValue(botInstanceId, out var existingAgent))
        {
            return existingAgent;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var botInstance = await unitOfWork.BotInstances.GetWithConfigsAsync(botInstanceId, ct);
        if (botInstance == null)
        {
            throw new InvalidOperationException($"Bot instance with ID {botInstanceId} not found");
        }

        var agent = await _agentFactory.CreateAgentAsync(botInstance, ct);
        _agents.TryAdd(botInstanceId, agent);
        return agent;
    }

    public void RemoveAgent(Guid botInstanceId)
    {
        _agents.TryRemove(botInstanceId, out _);
    }

    public bool HasAgent(Guid botInstanceId)
    {
        return _agents.ContainsKey(botInstanceId);
    }
}
