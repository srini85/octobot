using MediatR;

namespace OctoBot.Application.Commands.LLMConfig;

public record DeleteLLMConfigCommand(Guid Id) : IRequest<bool>;
