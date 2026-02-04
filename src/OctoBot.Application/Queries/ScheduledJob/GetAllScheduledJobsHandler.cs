using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.ScheduledJob;

public class GetAllScheduledJobsHandler : IRequestHandler<GetAllScheduledJobsQuery, IReadOnlyList<ScheduledJobDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllScheduledJobsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ScheduledJobDto>> Handle(GetAllScheduledJobsQuery request, CancellationToken cancellationToken)
    {
        var jobs = await _unitOfWork.ScheduledJobs.GetAllAsync(cancellationToken);
        var bots = await _unitOfWork.BotInstances.GetAllAsync(cancellationToken);
        var botDict = bots.ToDictionary(b => b.Id, b => b.Name);

        return jobs.Select(job => new ScheduledJobDto(
            job.Id,
            job.Name,
            job.Description,
            job.Instructions,
            job.CronExpression,
            job.BotInstanceId,
            botDict.GetValueOrDefault(job.BotInstanceId),
            job.TargetChannelConfigId,
            null,
            job.IsEnabled,
            job.LastRunAt,
            job.NextRunAt,
            job.LastRunStatus,
            job.CreatedAt,
            job.UpdatedAt
        )).ToList();
    }
}
