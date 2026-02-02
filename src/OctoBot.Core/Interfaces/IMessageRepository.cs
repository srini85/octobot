using OctoBot.Core.Entities;

namespace OctoBot.Core.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<IReadOnlyList<Message>> GetConversationHistoryAsync(Guid conversationId, int limit = 50, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Message> messages, CancellationToken ct = default);
}
