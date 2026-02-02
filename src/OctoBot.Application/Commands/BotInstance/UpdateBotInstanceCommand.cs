using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Commands.BotInstance;

public record UpdateBotInstanceCommand(Guid Id, UpdateBotInstanceDto Dto) : IRequest<BotInstanceDto?>;
