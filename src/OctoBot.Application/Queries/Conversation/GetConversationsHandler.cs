using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.Conversation;

public class GetConversationsHandler : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _unitOfWork.Conversations.GetByBotIdAsync(request.BotId, request.Skip, request.Take, cancellationToken);

        return conversations.Select(c => new ConversationDto(
            c.Id,
            c.BotInstanceId,
            c.ChannelId,
            c.UserId,
            c.Title,
            c.CreatedAt,
            c.LastMessageAt
        )).ToList();
    }
}
