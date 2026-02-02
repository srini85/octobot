using Microsoft.EntityFrameworkCore;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Infrastructure.Data;

namespace OctoBot.Infrastructure.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(OctoBotDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Message>> GetConversationHistoryAsync(Guid conversationId, int limit = 50, CancellationToken ct = default)
    {
        return await DbSet
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Message> messages, CancellationToken ct = default)
    {
        await DbSet.AddRangeAsync(messages, ct);
    }
}
