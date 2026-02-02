using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Core.ValueObjects;

namespace OctoBot.Infrastructure.Services;

public class ConversationMemory : IConversationMemory
{
    private readonly IUnitOfWork _unitOfWork;

    public ConversationMemory(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Conversation> GetOrCreateAsync(Guid botId, string channelId, string userId, CancellationToken ct = default)
    {
        var conversation = await _unitOfWork.Conversations.GetByChannelAndUserAsync(botId, channelId, userId, ct);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                BotInstanceId = botId,
                ChannelId = channelId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            await _unitOfWork.Conversations.AddAsync(conversation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return conversation;
    }

    public async Task AddMessageAsync(Guid conversationId, ChatMessage message, CancellationToken ct = default)
    {
        var dbMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = message.Role,
            Content = message.Content,
            Metadata = message.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(message.Metadata) : null,
            CreatedAt = message.Timestamp
        };

        await _unitOfWork.Messages.AddAsync(dbMessage, ct);

        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId, ct);
        if (conversation != null)
        {
            conversation.LastMessageAt = message.Timestamp;
            await _unitOfWork.Conversations.UpdateAsync(conversation, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, int limit = 50, CancellationToken ct = default)
    {
        var messages = await _unitOfWork.Messages.GetConversationHistoryAsync(conversationId, limit, ct);

        return messages.Select(m => new ChatMessage(
            m.Role,
            m.Content,
            m.CreatedAt,
            string.IsNullOrEmpty(m.Metadata) ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(m.Metadata)
        )).ToList();
    }

    public async Task ClearHistoryAsync(Guid conversationId, CancellationToken ct = default)
    {
        var messages = await _unitOfWork.Messages.GetConversationHistoryAsync(conversationId, int.MaxValue, ct);

        foreach (var message in messages)
        {
            await _unitOfWork.Messages.DeleteAsync(message, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
