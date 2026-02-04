using Cronos;
using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.ScheduledJob;

public class UpdateScheduledJobHandler : IRequestHandler<UpdateScheduledJobCommand, ScheduledJobDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateScheduledJobHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduledJobDto?> Handle(UpdateScheduledJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.ScheduledJobs.GetByIdAsync(request.Id, cancellationToken);
        if (job == null)
        {
            return null;
        }

        if (request.Dto.Name != null)
            job.Name = request.Dto.Name;

        if (request.Dto.Description != null)
            job.Description = request.Dto.Description;

        if (request.Dto.Instructions != null)
            job.Instructions = request.Dto.Instructions;

        if (request.Dto.CronExpression != null)
        {
            var cronExpression = CronExpression.Parse(request.Dto.CronExpression);
            job.CronExpression = request.Dto.CronExpression;
            job.NextRunAt = cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }

        if (request.Dto.TargetChannelConfigId != null)
            job.TargetChannelConfigId = request.Dto.TargetChannelConfigId;

        if (request.Dto.IsEnabled != null)
        {
            job.IsEnabled = request.Dto.IsEnabled.Value;
            if (job.IsEnabled && job.NextRunAt == null)
            {
                var cronExpression = CronExpression.Parse(job.CronExpression);
                job.NextRunAt = cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
            }
        }

        job.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ScheduledJobs.UpdateAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
