using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.BotInstance;

public record GetBotInstanceQuery(Guid Id) : IRequest<BotInstanceDto?>;
