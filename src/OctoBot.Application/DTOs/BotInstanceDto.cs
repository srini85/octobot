namespace OctoBot.Application.DTOs;

public record BotInstanceDto(
    Guid Id,
    string Name,
    string? Description,
    string? SystemPrompt,
    Guid? DefaultLLMConfigId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateBotInstanceDto(
    string Name,
    string? Description,
    string? SystemPrompt,
    Guid? DefaultLLMConfigId
);

public record UpdateBotInstanceDto(
    string? Name,
    string? Description,
    string? SystemPrompt,
    Guid? DefaultLLMConfigId,
    bool? IsActive
);
