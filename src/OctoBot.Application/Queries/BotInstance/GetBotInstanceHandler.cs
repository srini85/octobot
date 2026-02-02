using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.BotInstance;

public class GetBotInstanceHandler : IRequestHandler<GetBotInstanceQuery, BotInstanceDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBotInstanceHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BotInstanceDto?> Handle(GetBotInstanceQuery request, CancellationToken cancellationToken)
    {
        var bot = await _unitOfWork.BotInstances.GetByIdAsync(request.Id, cancellationToken);
        if (bot == null) return null;

        return new BotInstanceDto(
            bot.Id,
            bot.Name,
            bot.Description,
            bot.SystemPrompt,
            bot.DefaultLLMConfigId,
            bot.IsActive,
            bot.CreatedAt,
            bot.UpdatedAt
        );
    }
}
