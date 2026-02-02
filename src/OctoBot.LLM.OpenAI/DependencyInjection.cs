using Microsoft.Extensions.DependencyInjection;
using OctoBot.LLM.Abstractions;

namespace OctoBot.LLM.OpenAI;

public static class DependencyInjection
{
    public static IServiceCollection AddOpenAIProvider(this IServiceCollection services)
    {
        services.AddSingleton<ILLMProvider, OpenAIProvider>();
        return services;
    }
}
