using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;

    public PluginsController(IPluginRegistry registry, IUnitOfWork unitOfWork, IServiceScopeFactory scopeFactory)
    {
        _registry = registry;
        _unitOfWork = unitOfWork;
        _scopeFactory = scopeFactory;
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
            p.Metadata.ReadMe,
            p.Metadata.Dependencies,
            p.Metadata.Settings?.Select(s => new PluginSettingDefinitionDto(
                s.Key, s.DisplayName, s.Description, s.Type.ToString(), s.IsRequired, s.DefaultValue, s.Options
            )).ToList(),
            IsTestable: p is ITestablePlugin
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
            Dictionary<string, string>? settings = null;
            if (!string.IsNullOrEmpty(config?.Settings))
            {
                settings = JsonSerializer.Deserialize<Dictionary<string, string>>(config.Settings);
            }
            return new BotPluginStatusDto(
                p.Metadata.Id,
                p.Metadata.Name,
                p.Metadata.Description,
                p.Metadata.Version,
                config?.IsEnabled ?? false,
                settings
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
            if (dto.Settings != null)
            {
                existing.Settings = JsonSerializer.Serialize(dto.Settings);
            }
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
                Settings = dto.Settings != null ? JsonSerializer.Serialize(dto.Settings) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.PluginConfigs.AddAsync(config, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpPost("bot/{botId}/test-connection/{pluginId}")]
    public async Task<ActionResult> TestConnection(
        Guid botId,
        string pluginId,
        [FromBody] Dictionary<string, string>? requestSettings,
        CancellationToken ct)
    {
        var plugin = _registry.GetPlugin(pluginId);
        if (plugin == null)
            return NotFound($"Plugin '{pluginId}' not found");

        if (plugin is not ITestablePlugin testable)
            return BadRequest($"Plugin '{pluginId}' does not support connection testing.");

        // Load saved settings
        var configs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == pluginId, ct);
        var config = configs.FirstOrDefault();

        var settings = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(config?.Settings))
        {
            settings = JsonSerializer.Deserialize<Dictionary<string, string>>(config.Settings) ?? new();
        }

        // Merge request body settings (overrides saved)
        if (requestSettings != null)
        {
            foreach (var kv in requestSettings)
                settings[kv.Key] = kv.Value;
        }

        // Configure the plugin with merged settings
        if (plugin is IConfigurablePlugin configurable)
        {
            configurable.Configure(botId, settings, _scopeFactory);
        }

        var (success, message) = await testable.TestConnectionAsync();
        return Ok(new { success, message });
    }
}

public record BotPluginStatusDto(
    string Id,
    string Name,
    string Description,
    string Version,
    bool IsEnabled,
    Dictionary<string, string>? Settings = null
);

public record TogglePluginDto(
    string PluginId,
    bool IsEnabled,
    Dictionary<string, string>? Settings = null
);
