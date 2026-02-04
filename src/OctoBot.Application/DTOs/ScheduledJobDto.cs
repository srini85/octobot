namespace OctoBot.Application.DTOs;

public record ScheduledJobDto(
    Guid Id,
    string Name,
    string? Description,
    string Instructions,
    string CronExpression,
    Guid BotInstanceId,
    string? BotName,
    Guid? TargetChannelConfigId,
    string? TargetChannelType,
    bool IsEnabled,
    DateTime? LastRunAt,
    DateTime? NextRunAt,
    string? LastRunStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateScheduledJobDto(
    string Name,
    string? Description,
    string Instructions,
    string CronExpression,
    Guid BotInstanceId,
    Guid? TargetChannelConfigId
);

public record UpdateScheduledJobDto(
    string? Name,
    string? Description,
    string? Instructions,
    string? CronExpression,
    Guid? TargetChannelConfigId,
    bool? IsEnabled
);

public record JobExecutionDto(
    Guid Id,
    Guid ScheduledJobId,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Status,
    string? Output,
    string? ErrorMessage
);
