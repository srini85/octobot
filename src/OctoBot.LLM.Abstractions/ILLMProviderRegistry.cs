namespace OctoBot.LLM.Abstractions;

public interface ILLMProviderRegistry
{
    void Register(ILLMProvider provider);
    ILLMProvider? GetProvider(string name);
    IReadOnlyList<ILLMProvider> GetAllProviders();
    bool HasProvider(string name);
}
