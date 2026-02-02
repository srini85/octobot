using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.BotInstance;

public class CreateBotInstanceHandler : IRequestHandler<CreateBotInstanceCommand, BotInstanceDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateBotInstanceHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BotInstanceDto> Handle(CreateBotInstanceCommand request, CancellationToken cancellationToken)
    {
        var bot = new Core.Entities.BotInstance
        {
            Id = Guid.NewGuid(),
            Name = request.Dto.Name,
            Description = request.Dto.Description,
            SystemPrompt = request.Dto.SystemPrompt,
            DefaultLLMConfigId = request.Dto.DefaultLLMConfigId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.BotInstances.AddAsync(bot, cancellationToken);
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
