using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Queries.LLMConfig;

public record GetAllLLMConfigsQuery : IRequest<IReadOnlyList<LLMConfigDto>>;
