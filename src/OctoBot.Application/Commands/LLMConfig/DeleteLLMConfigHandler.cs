using MediatR;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.LLMConfig;

public class DeleteLLMConfigHandler : IRequestHandler<DeleteLLMConfigCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLLMConfigHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteLLMConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.LLMConfigs.GetByIdAsync(request.Id, cancellationToken);
        if (config == null)
        {
            return false;
        }

        await _unitOfWork.LLMConfigs.DeleteAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
