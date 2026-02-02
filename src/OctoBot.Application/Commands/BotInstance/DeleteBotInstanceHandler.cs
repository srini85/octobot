using MediatR;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.BotInstance;

public class DeleteBotInstanceHandler : IRequestHandler<DeleteBotInstanceCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBotInstanceHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteBotInstanceCommand request, CancellationToken cancellationToken)
    {
        var bot = await _unitOfWork.BotInstances.GetByIdAsync(request.Id, cancellationToken);
        if (bot == null) return false;

        await _unitOfWork.BotInstances.DeleteAsync(bot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
