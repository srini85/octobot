using OctoBot.Core.ValueObjects;

namespace OctoBot.Agent;

public interface IOctoBotAgent
{
    Guid BotInstanceId { get; }
    Task InitializeAsync(CancellationToken ct = default);
    Task<string> ProcessMessageAsync(IncomingMessage message, CancellationToken ct = default);
    IAsyncEnumerable<string> ProcessMessageStreamAsync(IncomingMessage message, CancellationToken ct = default);
}
