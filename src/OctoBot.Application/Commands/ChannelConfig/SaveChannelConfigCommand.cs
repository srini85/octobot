using MediatR;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Commands.ChannelConfig;

public record SaveChannelConfigCommand(CreateChannelConfigDto Dto) : IRequest<ChannelConfigDto>;
