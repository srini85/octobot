namespace OctoBot.Channels.Abstractions;

public interface IChannelFactory
{
    string ChannelType { get; }
    IChannel Create(ChannelConfiguration config);
    IReadOnlyList<ChannelSettingDefinition> GetSettingDefinitions();
}

public record ChannelSettingDefinition(
    string Key,
    string DisplayName,
    string Description,
    SettingType Type,
    bool IsRequired,
    string? DefaultValue = null
);

public enum SettingType
{
    String,
    Secret,
    Number,
    Boolean,
    Select
}
