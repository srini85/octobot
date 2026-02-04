using MediatR;
using Microsoft.AspNetCore.Mvc;
using OctoBot.Application.Commands.ScheduledJob;
using OctoBot.Application.DTOs;
using OctoBot.Application.Queries.ScheduledJob;
using OctoBot.Api.Services;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduledJobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ScheduledJobService _jobService;

    public ScheduledJobsController(IMediator mediator, ScheduledJobService jobService)
    {
        _mediator = mediator;
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScheduledJobDto>>> GetAll()
    {
        var jobs = await _mediator.Send(new GetAllScheduledJobsQuery());
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScheduledJobDto>> GetById(Guid id)
    {
        var job = await _mediator.Send(new GetScheduledJobQuery(id));
        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<ScheduledJobDto>> Create([FromBody] CreateScheduledJobDto dto)
    {
        try
        {
            var job = await _mediator.Send(new CreateScheduledJobCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
        }
        catch (Exception ex) when (ex.Message.Contains("cron", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid cron expression", message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ScheduledJobDto>> Update(Guid id, [FromBody] UpdateScheduledJobDto dto)
    {
        try
        {
            var job = await _mediator.Send(new UpdateScheduledJobCommand(id, dto));
            if (job == null)
            {
                return NotFound();
            }
            return Ok(job);
        }
        catch (Exception ex) when (ex.Message.Contains("cron", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid cron expression", message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteScheduledJobCommand(id));
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<ActionResult<ScheduledJobDto>> Toggle(Guid id, [FromBody] ToggleRequest request)
    {
        var job = await _mediator.Send(new UpdateScheduledJobCommand(id, new UpdateScheduledJobDto(
            null, null, null, null, null, request.Enabled
        )));

        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpGet("{id:guid}/executions")]
    public async Task<ActionResult<IReadOnlyList<JobExecutionDto>>> GetExecutions(Guid id, [FromQuery] int limit = 50)
    {
        var executions = await _mediator.Send(new GetJobExecutionsQuery(id, limit));
        return Ok(executions);
    }

    [HttpPost("{id:guid}/run")]
    public async Task<ActionResult> RunNow(Guid id)
    {
        var job = await _mediator.Send(new GetScheduledJobQuery(id));
        if (job == null)
        {
            return NotFound();
        }

        // Run the job asynchronously
        _ = Task.Run(async () =>
        {
            await _jobService.RunJobNowAsync(id);
        });

        return Accepted(new { message = "Job execution started" });
    }
}

public record ToggleRequest(bool Enabled);
