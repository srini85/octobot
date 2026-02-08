using Microsoft.Extensions.DependencyInjection;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Plugins.Office365;

public static class DependencyInjection
{
    public static IServiceCollection AddOffice365Plugins(this IServiceCollection services)
    {
        services.AddSingleton<IPlugin, Office365EmailPlugin>();
        return services;
    }
}
