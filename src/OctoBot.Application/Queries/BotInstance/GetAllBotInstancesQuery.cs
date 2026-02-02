using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.BotInstance;

public record GetAllBotInstancesQuery : IRequest<IReadOnlyList<BotInstanceDto>>;
