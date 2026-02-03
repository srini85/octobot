using Microsoft.AspNetCore.Mvc;
using OctoBot.Application.DTOs;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PluginsController : ControllerBase
{
    private readonly IPluginRegistry _registry;
    private readonly IUnitOfWork _unitOfWork;

    public PluginsController(IPluginRegistry registry, IUnitOfWork unitOfWork)
    {
        _registry = registry;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<PluginInfoDto>> GetPlugins()
    {
        var plugins = _registry.GetAllPlugins().Select(p => new PluginInfoDto(
            p.Metadata.Id,
            p.Metadata.Name,
            p.Metadata.Description,
            p.Metadata.Version,
            p.Metadata.Author,
            p.Metadata.Dependencies
        )).ToList();

        return Ok(plugins);
    }

    [HttpGet("bot/{botId}")]
    public async Task<ActionResult<IReadOnlyList<BotPluginStatusDto>>> GetBotPlugins(
        Guid botId,
        CancellationToken ct)
    {
        var allPlugins = _registry.GetAllPlugins();
        var botConfigs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId, ct);

        var result = allPlugins.Select(p =>
        {
            var config = botConfigs.FirstOrDefault(c => c.PluginId == p.Metadata.Id);
            return new BotPluginStatusDto(
                p.Metadata.Id,
                p.Metadata.Name,
                p.Metadata.Description,
                p.Metadata.Version,
                config?.IsEnabled ?? false
            );
        }).ToList();

        return Ok(result);
    }

    [HttpPost("bot/{botId}/toggle")]
    public async Task<ActionResult> TogglePlugin(
        Guid botId,
        [FromBody] TogglePluginDto dto,
        CancellationToken ct)
    {
        // Verify plugin exists
        var plugin = _registry.GetPlugin(dto.PluginId);
        if (plugin == null)
        {
            return NotFound($"Plugin '{dto.PluginId}' not found");
        }

        var existing = (await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == dto.PluginId, ct))
            .FirstOrDefault();

        if (existing != null)
        {
            existing.IsEnabled = dto.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.PluginConfigs.UpdateAsync(existing, ct);
        }
        else
        {
            var config = new PluginConfig
            {
                Id = Guid.NewGuid(),
                BotInstanceId = botId,
                PluginId = dto.PluginId,
                IsEnabled = dto.IsEnabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.PluginConfigs.AddAsync(config, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }
}

public record BotPluginStatusDto(
    string Id,
    string Name,
    string Description,
    string Version,
    bool IsEnabled
);

public record TogglePluginDto(
    string PluginId,
    bool IsEnabled
);
