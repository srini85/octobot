using Microsoft.Extensions.DependencyInjection;
using OctoBot.LLM.Abstractions;

namespace OctoBot.LLM.Anthropic;

public static class DependencyInjection
{
    public static IServiceCollection AddAnthropicProvider(this IServiceCollection services)
    {
        services.AddSingleton<ILLMProvider, AnthropicProvider>();
        return services;
    }
}
