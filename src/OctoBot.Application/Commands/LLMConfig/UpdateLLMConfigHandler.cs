using System.Text.Json;
using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.LLMConfig;

public class UpdateLLMConfigHandler : IRequestHandler<UpdateLLMConfigCommand, LLMConfigDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLLMConfigHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LLMConfigDto?> Handle(UpdateLLMConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.LLMConfigs.GetByIdAsync(request.Id, cancellationToken);
        if (config == null)
        {
            return null;
        }

        if (request.Dto.Name != null)
            config.Name = request.Dto.Name;

        if (request.Dto.ModelId != null)
            config.ModelId = request.Dto.ModelId;

        if (request.Dto.ApiKey != null)
            config.ApiKey = request.Dto.ApiKey;

        if (request.Dto.Endpoint != null)
            config.Endpoint = request.Dto.Endpoint;

        if (request.Dto.Settings != null)
            config.Settings = JsonSerializer.Serialize(request.Dto.Settings);

        config.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.LLMConfigs.UpdateAsync(config, cancellationToken);
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
