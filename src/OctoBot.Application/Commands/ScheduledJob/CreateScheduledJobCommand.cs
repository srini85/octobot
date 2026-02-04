using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Commands.ScheduledJob;

public record CreateScheduledJobCommand(CreateScheduledJobDto Dto) : IRequest<ScheduledJobDto>;
