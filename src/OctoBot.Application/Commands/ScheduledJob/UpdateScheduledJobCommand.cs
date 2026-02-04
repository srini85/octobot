using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Commands.ScheduledJob;

public record UpdateScheduledJobCommand(Guid Id, UpdateScheduledJobDto Dto) : IRequest<ScheduledJobDto?>;
