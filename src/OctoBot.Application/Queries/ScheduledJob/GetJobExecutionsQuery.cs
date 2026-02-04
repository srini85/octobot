using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.ScheduledJob;

public record GetJobExecutionsQuery(Guid JobId, int Limit = 50) : IRequest<IReadOnlyList<JobExecutionDto>>;
