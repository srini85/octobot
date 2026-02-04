using Cronos;
using OctoBot.Agent;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Core.ValueObjects;

namespace OctoBot.Api.Services;

public class ScheduledJobService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledJobService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

    public ScheduledJobService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled Job Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled jobs");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Scheduled Job Service stopped");
    }

    private async Task ProcessDueJobsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var dueJobs = await unitOfWork.ScheduledJobs.FindAsync(
            j => j.IsEnabled && j.NextRunAt != null && j.NextRunAt <= now, ct);

        if (dueJobs.Count > 0)
        {
            _logger.LogInformation("Found {Count} due jobs to execute", dueJobs.Count);
        }

        foreach (var job in dueJobs)
        {
            // Execute each job in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteJobAsync(job.Id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing job {JobId}", job.Id);
                }
            }, ct);
        }
    }

    public async Task ExecuteJobAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var agentManager = scope.ServiceProvider.GetRequiredService<IAgentManager>();

        var job = await unitOfWork.ScheduledJobs.GetByIdAsync(jobId, ct);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        _logger.LogInformation("Executing job {JobId}: {JobName}", job.Id, job.Name);

        var execution = new JobExecution
        {
            Id = Guid.NewGuid(),
            ScheduledJobId = job.Id,
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        try
        {
            await unitOfWork.JobExecutions.AddAsync(execution, ct);
            job.LastRunAt = DateTime.UtcNow;
            job.LastRunStatus = "Running";
            await unitOfWork.ScheduledJobs.UpdateAsync(job, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Get or create agent for the bot
            var agent = await agentManager.GetOrCreateAgentAsync(job.BotInstanceId, ct);

            // Create an incoming message from the job instructions
            var message = new IncomingMessage(
                ChannelType: "scheduled-job",
                ChannelId: $"job-{job.Id}",
                UserId: "system",
                UserName: "Scheduled Job",
                Content: job.Instructions,
                Timestamp: DateTime.UtcNow
            );

            // Process the message through the agent
            var response = await agent.ProcessMessageAsync(message, ct);

            // Update execution as completed
            execution.CompletedAt = DateTime.UtcNow;
            execution.Status = "Completed";
            execution.Output = response;

            job.LastRunStatus = "Success";
            _logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        {
            execution.CompletedAt = DateTime.UtcNow;
            execution.Status = "Failed";
            execution.ErrorMessage = ex.Message;
            job.LastRunStatus = "Failed";
            _logger.LogError(ex, "Job {JobId} failed", job.Id);
        }

        // Calculate next run time
        try
        {
            var cronExpr = CronExpression.Parse(job.CronExpression);
            job.NextRunAt = cronExpr.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse cron expression for job {JobId}", job.Id);
            job.NextRunAt = null;
        }

        job.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.JobExecutions.UpdateAsync(execution, ct);
        await unitOfWork.ScheduledJobs.UpdateAsync(job, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RunJobNowAsync(Guid jobId, CancellationToken ct = default)
    {
        await ExecuteJobAsync(jobId, ct);
    }
}
