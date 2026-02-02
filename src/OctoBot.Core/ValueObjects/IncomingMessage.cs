namespace OctoBot.Core.ValueObjects;

public record IncomingMessage(
    string ChannelType,
    string ChannelId,
    string UserId,
    string UserName,
    string Content,
    DateTime Timestamp,
    string? ReplyToMessageId = null,
    IReadOnlyList<Attachment>? Attachments = null,
    Dictionary<string, object>? Metadata = null
);

public record Attachment(
    string Type,
    string? Url,
    string? FileName,
    byte[]? Data
);
