using MediatR;

namespace OctoBot.Application.Commands.BotInstance;

public record DeleteBotInstanceCommand(Guid Id) : IRequest<bool>;
