using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.BotInstance;

public class UpdateBotInstanceHandler : IRequestHandler<UpdateBotInstanceCommand, BotInstanceDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBotInstanceHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BotInstanceDto?> Handle(UpdateBotInstanceCommand request, CancellationToken cancellationToken)
    {
        var bot = await _unitOfWork.BotInstances.GetByIdAsync(request.Id, cancellationToken);
        if (bot == null) return null;

        if (request.Dto.Name != null) bot.Name = request.Dto.Name;
        if (request.Dto.Description != null) bot.Description = request.Dto.Description;
        if (request.Dto.SystemPrompt != null) bot.SystemPrompt = request.Dto.SystemPrompt;
        if (request.Dto.DefaultLLMConfigId.HasValue) bot.DefaultLLMConfigId = request.Dto.DefaultLLMConfigId;
        if (request.Dto.IsActive.HasValue) bot.IsActive = request.Dto.IsActive.Value;
        bot.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.BotInstances.UpdateAsync(bot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
