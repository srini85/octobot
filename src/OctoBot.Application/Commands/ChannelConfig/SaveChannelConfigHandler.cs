using System.Text.Json;
using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Commands.ChannelConfig;

public class SaveChannelConfigHandler : IRequestHandler<SaveChannelConfigCommand, ChannelConfigDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SaveChannelConfigHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ChannelConfigDto> Handle(SaveChannelConfigCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // Check if config already exists for this bot and channel type
        var existingConfigs = await _unitOfWork.ChannelConfigs.FindAsync(
            c => c.BotInstanceId == dto.BotInstanceId && c.ChannelType == dto.ChannelType,
            cancellationToken);

        var existing = existingConfigs.FirstOrDefault();

        Core.Entities.ChannelConfig config;

        if (existing != null)
        {
            // Update existing
            existing.IsEnabled = dto.IsEnabled;
            existing.Settings = JsonSerializer.Serialize(dto.Settings);
            existing.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.ChannelConfigs.UpdateAsync(existing, cancellationToken);
            config = existing;
        }
        else
        {
            // Create new
            config = new Core.Entities.ChannelConfig
            {
                Id = Guid.NewGuid(),
                BotInstanceId = dto.BotInstanceId,
                ChannelType = dto.ChannelType,
                IsEnabled = dto.IsEnabled,
                Settings = JsonSerializer.Serialize(dto.Settings),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ChannelConfigs.AddAsync(config, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var settings = string.IsNullOrEmpty(config.Settings)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(config.Settings) ?? new Dictionary<string, string>();

        return new ChannelConfigDto(
            config.Id,
            config.BotInstanceId,
            config.ChannelType,
            config.IsEnabled,
            settings,
            config.CreatedAt,
            config.UpdatedAt
        );
    }
}
