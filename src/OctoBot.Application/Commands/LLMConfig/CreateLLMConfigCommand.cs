using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Commands.LLMConfig;

public record CreateLLMConfigCommand(CreateLLMConfigDto Dto) : IRequest<LLMConfigDto>;
