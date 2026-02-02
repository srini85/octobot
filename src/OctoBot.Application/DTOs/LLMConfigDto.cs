namespace OctoBot.Application.DTOs;

public record LLMConfigDto(
    Guid Id,
    string Name,
    string ProviderType,
    string? ModelId,
    string? Endpoint,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateLLMConfigDto(
    string Name,
    string ProviderType,
    string? ModelId,
    string? ApiKey,
    string? Endpoint,
    Dictionary<string, object>? Settings
);

public record UpdateLLMConfigDto(
    string? Name,
    string? ModelId,
    string? ApiKey,
    string? Endpoint,
    Dictionary<string, object>? Settings
);
