using System.Text.Json;
using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.LLMConfig;

public class CreateLLMConfigHandler : IRequestHandler<CreateLLMConfigCommand, LLMConfigDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateLLMConfigHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LLMConfigDto> Handle(CreateLLMConfigCommand request, CancellationToken cancellationToken)
    {
        var config = new Core.Entities.LLMConfig
        {
            Id = Guid.NewGuid(),
            Name = request.Dto.Name,
            ProviderType = request.Dto.ProviderType,
            ModelId = request.Dto.ModelId,
            ApiKey = request.Dto.ApiKey,
            Endpoint = request.Dto.Endpoint,
            Settings = request.Dto.Settings != null ? JsonSerializer.Serialize(request.Dto.Settings) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.LLMConfigs.AddAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LLMConfigDto(
            config.Id,
            config.Name,
            config.ProviderType,
            config.ModelId,
            config.Endpoint,
            config.CreatedAt,
            config.UpdatedAt
        );
    }
}
