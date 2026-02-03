using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Commands.LLMConfig;

public record UpdateLLMConfigCommand(Guid Id, UpdateLLMConfigDto Dto) : IRequest<LLMConfigDto?>;
