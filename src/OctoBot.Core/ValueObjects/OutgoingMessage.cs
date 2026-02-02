namespace OctoBot.Core.ValueObjects;

public record OutgoingMessage(
    string ChannelId,
    string UserId,
    string Content,
    string? ReplyToMessageId = null,
    IReadOnlyList<OutgoingAttachment>? Attachments = null,
    MessageFormat Format = MessageFormat.Plain,
    Dictionary<string, object>? Metadata = null
);

public record OutgoingAttachment(
    string Type,
    string FileName,
    byte[] Data,
    string? Caption = null
);

public enum MessageFormat
{
    Plain,
    Markdown,
    Html
}
