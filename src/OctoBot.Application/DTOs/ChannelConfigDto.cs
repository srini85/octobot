namespace OctoBot.Application.DTOs;

public record ChannelConfigDto(
    Guid Id,
    Guid BotInstanceId,
    string ChannelType,
    bool IsEnabled,
    Dictionary<string, string> Settings,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateChannelConfigDto(
    Guid BotInstanceId,
    string ChannelType,
    bool IsEnabled,
    Dictionary<string, string> Settings
);

public record UpdateChannelConfigDto(
    bool? IsEnabled,
    Dictionary<string, string>? Settings
);
