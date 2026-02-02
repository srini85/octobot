using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.BotInstance;

public class GetAllBotInstancesHandler : IRequestHandler<GetAllBotInstancesQuery, IReadOnlyList<BotInstanceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllBotInstancesHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BotInstanceDto>> Handle(GetAllBotInstancesQuery request, CancellationToken cancellationToken)
    {
        var bots = await _unitOfWork.BotInstances.GetAllAsync(cancellationToken);

        return bots.Select(bot => new BotInstanceDto(
            bot.Id,
            bot.Name,
            bot.Description,
            bot.SystemPrompt,
            bot.DefaultLLMConfigId,
            bot.IsActive,
            bot.CreatedAt,
            bot.UpdatedAt
        )).ToList();
    }
}
