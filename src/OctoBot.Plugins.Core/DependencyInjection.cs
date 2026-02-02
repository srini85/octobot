using Microsoft.Extensions.DependencyInjection;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Plugins.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCorePlugins(this IServiceCollection services)
    {
        services.AddSingleton<IPlugin, DateTimePlugin>();
        services.AddSingleton<IPlugin, MathPlugin>();
        services.AddSingleton<IPlugin, WebSearchPlugin>();
        return services;
    }
}
