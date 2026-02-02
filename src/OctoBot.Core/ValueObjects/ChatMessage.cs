using OctoBot.Core.Entities;

namespace OctoBot.Core.ValueObjects;

public record ChatMessage(
    MessageRole Role,
    string Content,
    DateTime Timestamp,
    Dictionary<string, object>? Metadata = null
);
