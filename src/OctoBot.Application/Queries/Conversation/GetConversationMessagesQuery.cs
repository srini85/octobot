using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.Conversation;

public record GetConversationMessagesQuery(Guid ConversationId, int Limit = 50) : IRequest<ConversationWithMessagesDto?>;
