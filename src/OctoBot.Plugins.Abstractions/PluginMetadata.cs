namespace OctoBot.Plugins.Abstractions;

public record PluginMetadata(
    string Id,
    string Name,
    string Description,
    string Version,
    string? Author = null,
    IReadOnlyList<string>? Dependencies = null,
    IReadOnlyList<PluginSettingDefinition>? Settings = null
);

public record PluginSettingDefinition(
    string Key,
    string DisplayName,
    string Description,
    PluginSettingType Type,
    bool IsRequired,
    string? DefaultValue = null
);

public enum PluginSettingType
{
    String,
    Secret,
    Number,
    Boolean,
    Select
}
