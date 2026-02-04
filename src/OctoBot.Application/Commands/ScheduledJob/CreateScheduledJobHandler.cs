using Cronos;
using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.ScheduledJob;

public class CreateScheduledJobHandler : IRequestHandler<CreateScheduledJobCommand, ScheduledJobDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateScheduledJobHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduledJobDto> Handle(CreateScheduledJobCommand request, CancellationToken cancellationToken)
    {
        var bot = await _unitOfWork.BotInstances.GetByIdAsync(request.Dto.BotInstanceId, cancellationToken);
        if (bot == null)
        {
            throw new InvalidOperationException($"Bot instance {request.Dto.BotInstanceId} not found");
        }

        // Parse and validate cron expression
        var cronExpression = CronExpression.Parse(request.Dto.CronExpression);
        var nextRun = cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);

        var job = new Core.Entities.ScheduledJob
        {
            Id = Guid.NewGuid(),
            Name = request.Dto.Name,
            Description = request.Dto.Description,
            Instructions = request.Dto.Instructions,
            CronExpression = request.Dto.CronExpression,
            BotInstanceId = request.Dto.BotInstanceId,
            TargetChannelConfigId = request.Dto.TargetChannelConfigId,
            IsEnabled = true,
            NextRunAt = nextRun,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ScheduledJobs.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ScheduledJobDto(
            job.Id,
            job.Name,
            job.Description,
            job.Instructions,
            job.CronExpression,
            job.BotInstanceId,
            bot.Name,
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
