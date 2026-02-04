using MediatR;

namespace OctoBot.Application.Commands.ScheduledJob;

public record DeleteScheduledJobCommand(Guid Id) : IRequest<bool>;
