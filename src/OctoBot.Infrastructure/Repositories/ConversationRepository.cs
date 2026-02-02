using Microsoft.EntityFrameworkCore;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Infrastructure.Data;

namespace OctoBot.Infrastructure.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(OctoBotDbContext context) : base(context)
    {
    }

    public async Task<Conversation?> GetByChannelAndUserAsync(Guid botId, string channelId, string userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.BotInstanceId == botId && c.ChannelId == channelId && c.UserId == userId, ct);
    }

    public async Task<Conversation?> GetWithMessagesAsync(Guid id, int messageLimit = 50, CancellationToken ct = default)
    {
        var conversation = await DbSet.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (conversation == null) return null;

        var messages = await Context.Messages
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(messageLimit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        conversation.Messages = messages;
        return conversation;
    }

    public async Task<IReadOnlyList<Conversation>> GetByBotIdAsync(Guid botId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        return await DbSet
            .Where(c => c.BotInstanceId == botId)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }
}
