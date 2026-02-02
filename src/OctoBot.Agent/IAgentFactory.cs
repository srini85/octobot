using OctoBot.Core.Entities;

namespace OctoBot.Agent;

public interface IAgentFactory
{
    Task<IOctoBotAgent> CreateAgentAsync(BotInstance botInstance, CancellationToken ct = default);
}
