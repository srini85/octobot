using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OctoBot.Agent;
using OctoBot.Channels.Abstractions;
using OctoBot.Core.Interfaces;
using OctoBot.Core.ValueObjects;

namespace OctoBot.Application.Services;

public interface IChannelManager
{
    Task<bool> StartChannelAsync(Guid botInstanceId, string channelType, CancellationToken ct = default);
    Task StopChannelAsync(Guid botInstanceId, string channelType, CancellationToken ct = default);
    bool IsChannelRunning(Guid botInstanceId, string channelType);
    IReadOnlyList<RunningChannelInfo> GetRunningChannels(Guid botInstanceId);
}

public record RunningChannelInfo(string ChannelType, ChannelStatus Status, DateTime StartedAt);

public class ChannelManager : IChannelManager, IDisposable
{
    private readonly IChannelRegistry _channelRegistry;
    private readonly IAgentManager _agentManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, RunningChannel> _runningChannels = new();

    public ChannelManager(
        IChannelRegistry channelRegistry,
        IAgentManager agentManager,
        IServiceScopeFactory scopeFactory)
    {
        _channelRegistry = channelRegistry;
        _agentManager = agentManager;
        _scopeFactory = scopeFactory;
    }

    private static string GetKey(Guid botInstanceId, string channelType) => $"{botInstanceId}:{channelType}";

    public async Task<bool> StartChannelAsync(Guid botInstanceId, string channelType, CancellationToken ct = default)
    {
        var key = GetKey(botInstanceId, channelType);

        if (_runningChannels.ContainsKey(key))
        {
            return true; // Already running
        }

        // Get channel config from database
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var configs = await unitOfWork.ChannelConfigs.FindAsync(
            c => c.BotInstanceId == botInstanceId && c.ChannelType == channelType,
            ct);

        var config = configs.FirstOrDefault();
        if (config == null)
        {
            throw new InvalidOperationException($"No configuration found for channel type '{channelType}' on bot '{botInstanceId}'");
        }

        var settings = string.IsNullOrEmpty(config.Settings)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(config.Settings) ?? new Dictionary<string, string>();

        // Create channel instance
        var factory = _channelRegistry.GetFactory(channelType);
        if (factory == null)
        {
            throw new InvalidOperationException($"No factory found for channel type '{channelType}'");
        }

        var channelConfig = new ChannelConfiguration
        {
            BotInstanceId = botInstanceId,
            ChannelType = channelType,
            Settings = settings
        };

        var channel = factory.Create(channelConfig);

        // Wire up message handler
        channel.OnMessageReceived += async (message) =>
        {
            await HandleIncomingMessageAsync(botInstanceId, channel, message);
        };

        channel.OnError += async (error) =>
        {
            // Log error - could add more sophisticated error handling
            Console.WriteLine($"Channel error: {error.Message}");
            await Task.CompletedTask;
        };

        // Start the channel
        await channel.StartAsync(ct);

        var runningChannel = new RunningChannel(channel, DateTime.UtcNow);
        _runningChannels.TryAdd(key, runningChannel);

        return true;
    }

    public async Task StopChannelAsync(Guid botInstanceId, string channelType, CancellationToken ct = default)
    {
        var key = GetKey(botInstanceId, channelType);

        if (_runningChannels.TryRemove(key, out var runningChannel))
        {
            await runningChannel.Channel.StopAsync(ct);
            if (runningChannel.Channel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public bool IsChannelRunning(Guid botInstanceId, string channelType)
    {
        var key = GetKey(botInstanceId, channelType);
        return _runningChannels.ContainsKey(key);
    }

    public IReadOnlyList<RunningChannelInfo> GetRunningChannels(Guid botInstanceId)
    {
        var prefix = $"{botInstanceId}:";
        return _runningChannels
            .Where(kvp => kvp.Key.StartsWith(prefix))
            .Select(kvp => new RunningChannelInfo(
                kvp.Value.Channel.Name,
                kvp.Value.Channel.Status,
                kvp.Value.StartedAt))
            .ToList();
    }

    private async Task HandleIncomingMessageAsync(Guid botInstanceId, IChannel channel, IncomingMessage message)
    {
        try
        {
            var agent = await _agentManager.GetOrCreateAgentAsync(botInstanceId);
            var response = await agent.ProcessMessageAsync(message);

            var outgoing = new OutgoingMessage(
                ChannelId: message.ChannelId,
                UserId: message.UserId,
                Content: response
            );

            await channel.SendMessageAsync(outgoing);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");

            // Send error message back to user
            var errorMessage = new OutgoingMessage(
                ChannelId: message.ChannelId,
                UserId: message.UserId,
                Content: "Sorry, I encountered an error processing your message. Please try again."
            );

            try
            {
                await channel.SendMessageAsync(errorMessage);
            }
            catch
            {
                // Ignore errors when sending error message
            }
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _runningChannels)
        {
            kvp.Value.Channel.StopAsync().GetAwaiter().GetResult();
            if (kvp.Value.Channel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _runningChannels.Clear();
    }

    private record RunningChannel(IChannel Channel, DateTime StartedAt);
}
