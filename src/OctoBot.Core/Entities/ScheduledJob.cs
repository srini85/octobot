namespace OctoBot.Core.Entities;

public class ScheduledJob
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Instructions { get; set; }
    public required string CronExpression { get; set; }
    public Guid BotInstanceId { get; set; }
    public Guid? TargetChannelConfigId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public BotInstance? BotInstance { get; set; }
    public ChannelConfig? TargetChannelConfig { get; set; }
    public ICollection<JobExecution> Executions { get; set; } = [];
}
