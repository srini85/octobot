using Microsoft.Extensions.DependencyInjection;

namespace OctoBot.Agent;

public static class DependencyInjection
{
    public static IServiceCollection AddAgent(this IServiceCollection services)
    {
        services.AddSingleton<IAgentFactory, AgentFactory>();
        services.AddSingleton<IAgentManager, AgentManager>();
        return services;
    }
}
