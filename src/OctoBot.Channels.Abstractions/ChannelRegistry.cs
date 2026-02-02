using System.Collections.Concurrent;

namespace OctoBot.Channels.Abstractions;

public class ChannelRegistry : IChannelRegistry
{
    private readonly ConcurrentDictionary<string, IChannelFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    public ChannelRegistry(IEnumerable<IChannelFactory> factories)
    {
        foreach (var factory in factories)
        {
            Register(factory);
        }
    }

    public void Register(IChannelFactory factory)
    {
        _factories.TryAdd(factory.ChannelType, factory);
    }

    public IChannelFactory? GetFactory(string channelType)
    {
        _factories.TryGetValue(channelType, out var factory);
        return factory;
    }

    public IReadOnlyList<IChannelFactory> GetAllFactories()
    {
        return _factories.Values.ToList();
    }

    public bool HasChannel(string channelType)
    {
        return _factories.ContainsKey(channelType);
    }
}
