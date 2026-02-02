using OctoBot.Core.Entities;

namespace OctoBot.Core.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByChannelAndUserAsync(Guid botId, string channelId, string userId, CancellationToken ct = default);
    Task<Conversation?> GetWithMessagesAsync(Guid id, int messageLimit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> GetByBotIdAsync(Guid botId, int skip = 0, int take = 50, CancellationToken ct = default);
}
