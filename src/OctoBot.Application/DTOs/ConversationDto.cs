using OctoBot.Core.Entities;

namespace OctoBot.Application.DTOs;

public record ConversationDto(
    Guid Id,
    Guid BotInstanceId,
    string ChannelId,
    string UserId,
    string? Title,
    DateTime CreatedAt,
    DateTime LastMessageAt
);

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    MessageRole Role,
    string Content,
    DateTime CreatedAt
);

public record ConversationWithMessagesDto(
    Guid Id,
    Guid BotInstanceId,
    string ChannelId,
    string UserId,
    string? Title,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    IReadOnlyList<MessageDto> Messages
);
