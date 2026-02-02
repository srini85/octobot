using OctoBot.Core.Entities;

namespace OctoBot.Core.Interfaces;

public interface IBotInstanceRepository : IRepository<BotInstance>
{
    Task<BotInstance?> GetWithConfigsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BotInstance>> GetActiveBotsAsync(CancellationToken ct = default);
}
