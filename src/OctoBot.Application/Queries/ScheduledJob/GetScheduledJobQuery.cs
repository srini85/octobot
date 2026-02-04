using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.ScheduledJob;

public record GetScheduledJobQuery(Guid Id) : IRequest<ScheduledJobDto?>;
