using System.Collections.Concurrent;

namespace OctoBot.LLM.Abstractions;

public class LLMProviderRegistry : ILLMProviderRegistry
{
    private readonly ConcurrentDictionary<string, ILLMProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public LLMProviderRegistry(IEnumerable<ILLMProvider> providers)
    {
        foreach (var provider in providers)
        {
            Register(provider);
        }
    }

    public void Register(ILLMProvider provider)
    {
        _providers.TryAdd(provider.Name, provider);
    }

    public ILLMProvider? GetProvider(string name)
    {
        _providers.TryGetValue(name, out var provider);
        return provider;
    }

    public IReadOnlyList<ILLMProvider> GetAllProviders()
    {
        return _providers.Values.ToList();
    }

    public bool HasProvider(string name)
    {
        return _providers.ContainsKey(name);
    }
}
