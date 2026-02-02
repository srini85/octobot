using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Commands.BotInstance;

public record CreateBotInstanceCommand(CreateBotInstanceDto Dto) : IRequest<BotInstanceDto>;
