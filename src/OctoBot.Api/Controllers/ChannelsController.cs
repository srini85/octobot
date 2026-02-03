using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OctoBot.Application.Commands.ChannelConfig;
using OctoBot.Application.DTOs;
using OctoBot.Application.Services;
using OctoBot.Channels.Abstractions;
using OctoBot.Core.Interfaces;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelsController : ControllerBase
{
    private readonly IChannelRegistry _registry;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChannelManager _channelManager;

    public ChannelsController(
        IChannelRegistry registry,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        IChannelManager channelManager)
    {
        _registry = registry;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _channelManager = channelManager;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<ChannelInfo>> GetChannels()
    {
        var channels = _registry.GetAllFactories().Select(f => new ChannelInfo(
            f.ChannelType,
            f.GetSettingDefinitions()
        )).ToList();

        return Ok(channels);
    }

    [HttpGet("config/{botId}")]
    public async Task<ActionResult<IReadOnlyList<ChannelConfigDto>>> GetChannelConfigs(
        Guid botId,
        CancellationToken ct)
    {
        var configs = await _unitOfWork.ChannelConfigs.FindAsync(c => c.BotInstanceId == botId, ct);

        var dtos = configs.Select(c => new ChannelConfigDto(
            c.Id,
            c.BotInstanceId,
            c.ChannelType,
            c.IsEnabled,
            string.IsNullOrEmpty(c.Settings)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(c.Settings) ?? new Dictionary<string, string>(),
            c.CreatedAt,
            c.UpdatedAt
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost("config")]
    public async Task<ActionResult<ChannelConfigDto>> SaveChannelConfig(
        [FromBody] CreateChannelConfigDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SaveChannelConfigCommand(dto), ct);
        return Ok(result);
    }

    [HttpPost("{botId}/start/{channelType}")]
    public async Task<ActionResult> StartChannel(
        Guid botId,
        string channelType,
        CancellationToken ct)
    {
        try
        {
            await _channelManager.StartChannelAsync(botId, channelType, ct);
            return Ok(new { success = true, message = $"Channel '{channelType}' started successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{botId}/stop/{channelType}")]
    public async Task<ActionResult> StopChannel(
        Guid botId,
        string channelType,
        CancellationToken ct)
    {
        await _channelManager.StopChannelAsync(botId, channelType, ct);
        return Ok(new { success = true, message = $"Channel '{channelType}' stopped" });
    }

    [HttpGet("{botId}/status")]
    public ActionResult<IReadOnlyList<ChannelStatusInfo>> GetChannelStatus(Guid botId)
    {
        var running = _channelManager.GetRunningChannels(botId);
        var allChannels = _registry.GetAllFactories();

        var statuses = allChannels.Select(f => new ChannelStatusInfo(
            f.ChannelType,
            _channelManager.IsChannelRunning(botId, f.ChannelType),
            running.FirstOrDefault(r => r.ChannelType == f.ChannelType)?.Status.ToString() ?? "Stopped",
            running.FirstOrDefault(r => r.ChannelType == f.ChannelType)?.StartedAt
        )).ToList();

        return Ok(statuses);
    }
}

public record ChannelInfo(
    string ChannelType,
    IReadOnlyList<ChannelSettingDefinition> Settings
);

public record ChannelStatusInfo(
    string ChannelType,
    bool IsRunning,
    string Status,
    DateTime? StartedAt
);
