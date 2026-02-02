namespace OctoBot.Channels.Abstractions;

public record ChannelError(
    string ChannelName,
    string Message,
    Exception? Exception = null,
    bool IsRecoverable = true
);
