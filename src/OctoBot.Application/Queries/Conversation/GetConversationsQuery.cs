using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.Conversation;

public record GetConversationsQuery(Guid BotId, int Skip = 0, int Take = 50) : IRequest<IReadOnlyList<ConversationDto>>;
