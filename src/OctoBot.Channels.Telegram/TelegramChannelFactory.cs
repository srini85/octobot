using OctoBot.Channels.Abstractions;

namespace OctoBot.Channels.Telegram;

public class TelegramChannelFactory : IChannelFactory
{
    public string ChannelType => "telegram";

    public IChannel Create(ChannelConfiguration config)
    {
        return new TelegramChannel(config);
    }

    public IReadOnlyList<ChannelSettingDefinition> GetSettingDefinitions()
    {
        return new[]
        {
            new ChannelSettingDefinition(
                Key: "BotToken",
                DisplayName: "Bot Token",
                Description: "The Telegram bot token from @BotFather",
                Type: SettingType.Secret,
                IsRequired: true
            )
        };
    }
}
