namespace OctoBot.Application.DTOs;

public record PluginConfigDto(
    Guid Id,
    Guid BotInstanceId,
    string PluginId,
    bool IsEnabled,
    Dictionary<string, string> Settings,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UpdatePluginConfigDto(
    bool? IsEnabled,
    Dictionary<string, string>? Settings
);

public record PluginInfoDto(
    string Id,
    string Name,
    string Description,
    string Version,
    string? Author,
    IReadOnlyList<string>? Dependencies,
    IReadOnlyList<PluginSettingDefinitionDto>? Settings = null
);

public record PluginSettingDefinitionDto(
    string Key,
    string DisplayName,
    string Description,
    string Type,
    bool IsRequired,
    string? DefaultValue
);
