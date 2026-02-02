using OctoBot.Core.Entities;
using OctoBot.Core.ValueObjects;

namespace OctoBot.Core.Interfaces;

public interface IConversationMemory
{
    Task<Conversation> GetOrCreateAsync(Guid botId, string channelId, string userId, CancellationToken ct = default);
    Task AddMessageAsync(Guid conversationId, ChatMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, int limit = 50, CancellationToken ct = default);
    Task ClearHistoryAsync(Guid conversationId, CancellationToken ct = default);
}
