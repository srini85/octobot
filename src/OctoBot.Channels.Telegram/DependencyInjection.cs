using Microsoft.Extensions.DependencyInjection;
using OctoBot.Channels.Abstractions;

namespace OctoBot.Channels.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramChannel(this IServiceCollection services)
    {
        services.AddSingleton<IChannelFactory, TelegramChannelFactory>();
        return services;
    }
}
