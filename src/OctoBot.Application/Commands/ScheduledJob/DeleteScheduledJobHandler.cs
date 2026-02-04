using MediatR;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.ScheduledJob;

public class DeleteScheduledJobHandler : IRequestHandler<DeleteScheduledJobCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteScheduledJobHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteScheduledJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.ScheduledJobs.GetByIdAsync(request.Id, cancellationToken);
        if (job == null)
        {
            return false;
        }

        await _unitOfWork.ScheduledJobs.DeleteAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
