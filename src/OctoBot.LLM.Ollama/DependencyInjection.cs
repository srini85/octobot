using Microsoft.Extensions.DependencyInjection;
using OctoBot.LLM.Abstractions;

namespace OctoBot.LLM.Ollama;

public static class DependencyInjection
{
    public static IServiceCollection AddOllamaProvider(this IServiceCollection services)
    {
        services.AddSingleton<ILLMProvider, OllamaProvider>();
        return services;
    }
}
