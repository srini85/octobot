using Microsoft.EntityFrameworkCore;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Infrastructure.Data;

namespace OctoBot.Infrastructure.Repositories;

public class BotInstanceRepository : Repository<BotInstance>, IBotInstanceRepository
{
    public BotInstanceRepository(OctoBotDbContext context) : base(context)
    {
    }

    public async Task<BotInstance?> GetWithConfigsAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet
            .Include(b => b.DefaultLLMConfig)
            .Include(b => b.ChannelConfigs)
            .Include(b => b.PluginConfigs)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<IReadOnlyList<BotInstance>> GetActiveBotsAsync(CancellationToken ct = default)
    {
        return await DbSet
            .Where(b => b.IsActive)
            .Include(b => b.DefaultLLMConfig)
            .Include(b => b.ChannelConfigs)
            .Include(b => b.PluginConfigs)
            .ToListAsync(ct);
    }
}
