using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.ScheduledJob;

public class GetScheduledJobHandler : IRequestHandler<GetScheduledJobQuery, ScheduledJobDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetScheduledJobHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduledJobDto?> Handle(GetScheduledJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.ScheduledJobs.GetByIdAsync(request.Id, cancellationToken);
        if (job == null)
        {
            return null;
        }

        var bot = await _unitOfWork.BotInstances.GetByIdAsync(job.BotInstanceId, cancellationToken);

        return new ScheduledJobDto(
            job.Id,
            job.Name,
            job.Description,
            job.Instructions,
            job.CronExpression,
            job.BotInstanceId,
            bot?.Name,
            job.TargetChannelConfigId,
            null,
            job.IsEnabled,
            job.LastRunAt,
            job.NextRunAt,
            job.LastRunStatus,
            job.CreatedAt,
            job.UpdatedAt
        );
    }
}
