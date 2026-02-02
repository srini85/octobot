using OctoBot.Core.ValueObjects;

namespace OctoBot.Channels.Abstractions;

public interface IChannel
{
    string Name { get; }
    string DisplayName { get; }
    bool IsConnected { get; }
    ChannelStatus Status { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    event Func<IncomingMessage, Task>? OnMessageReceived;
    event Func<ChannelError, Task>? OnError;

    Task SendMessageAsync(OutgoingMessage message, CancellationToken ct = default);
    Task<bool> ValidateConfigurationAsync(ChannelConfiguration config, CancellationToken ct = default);
}
