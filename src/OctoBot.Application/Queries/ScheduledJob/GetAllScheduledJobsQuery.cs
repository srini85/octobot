using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.ScheduledJob;

public record GetAllScheduledJobsQuery : IRequest<IReadOnlyList<ScheduledJobDto>>;
