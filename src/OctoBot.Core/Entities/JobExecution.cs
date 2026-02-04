namespace OctoBot.Core.Entities;

public class JobExecution
{
    public Guid Id { get; set; }
    public Guid ScheduledJobId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public required string Status { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation property
    public ScheduledJob? ScheduledJob { get; set; }
}
