namespace OctoBot.Channels.Abstractions;

public interface IChannelRegistry
{
    void Register(IChannelFactory factory);
    IChannelFactory? GetFactory(string channelType);
    IReadOnlyList<IChannelFactory> GetAllFactories();
    bool HasChannel(string channelType);
}
