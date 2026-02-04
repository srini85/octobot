using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.ScheduledJob;

public class GetJobExecutionsHandler : IRequestHandler<GetJobExecutionsQuery, IReadOnlyList<JobExecutionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetJobExecutionsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<JobExecutionDto>> Handle(GetJobExecutionsQuery request, CancellationToken cancellationToken)
    {
        var executions = await _unitOfWork.JobExecutions.FindAsync(
            e => e.ScheduledJobId == request.JobId,
            cancellationToken);

        return executions
            .OrderByDescending(e => e.StartedAt)
            .Take(request.Limit)
            .Select(e => new JobExecutionDto(
                e.Id,
                e.ScheduledJobId,
                e.StartedAt,
                e.CompletedAt,
                e.Status,
                e.Output,
                e.ErrorMessage
            ))
            .ToList();
    }
}
