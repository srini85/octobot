using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.Conversation;

public class GetConversationMessagesHandler : IRequestHandler<GetConversationMessagesQuery, ConversationWithMessagesDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationMessagesHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationWithMessagesDto?> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _unitOfWork.Conversations.GetWithMessagesAsync(request.ConversationId, request.Limit, cancellationToken);
        if (conversation == null) return null;

        var messageDtos = conversation.Messages.Select(m => new MessageDto(
            m.Id,
            m.ConversationId,
            m.Role,
            m.Content,
            m.CreatedAt
        )).ToList();

        return new ConversationWithMessagesDto(
            conversation.Id,
            conversation.BotInstanceId,
            conversation.ChannelId,
            conversation.UserId,
            conversation.Title,
            conversation.CreatedAt,
            conversation.LastMessageAt,
            messageDtos
        );
    }
}
