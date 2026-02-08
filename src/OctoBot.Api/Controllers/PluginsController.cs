using System.Net.Http.Json;
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

    // ── OAuth flow for Microsoft Graph plugins ──

    private async Task<Dictionary<string, string>> GetPluginSettingsAsync(Guid botId, string pluginId, CancellationToken ct = default)
    {
        var pluginConfigs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == pluginId, ct);
        var pluginConfig = pluginConfigs.FirstOrDefault();
        if (pluginConfig == null || string.IsNullOrEmpty(pluginConfig.Settings))
            return new Dictionary<string, string>();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(pluginConfig.Settings) ?? new();
    }

    [HttpGet("oauth/auth-url")]
    public async Task<ActionResult> GetOAuthAuthUrl(
        [FromQuery] Guid botId,
        [FromQuery] string pluginId,
        CancellationToken ct)
    {
        var settings = await GetPluginSettingsAsync(botId, pluginId, ct);
        settings.TryGetValue("ClientId", out var clientId);
        settings.TryGetValue("TenantId", out var tenantId);
        if (string.IsNullOrEmpty(tenantId)) tenantId = "common";

        if (string.IsNullOrEmpty(clientId))
        {
            return BadRequest(new { error = "Client ID is not configured. Please save it in the plugin settings first." });
        }

        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/plugins/oauth/callback";

        var state = $"{botId}|{pluginId}";
        var authUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"
            + $"?client_id={Uri.EscapeDataString(clientId)}"
            + $"&response_type=code"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&scope={Uri.EscapeDataString("offline_access Mail.Read Mail.Send User.Read")}"
            + $"&state={Uri.EscapeDataString(state)}"
            + $"&response_mode=query";

        return Ok(new { authUrl, redirectUri });
    }

    [HttpGet("oauth/callback")]
    public async Task<ContentResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return Content(BuildCallbackHtml(false, $"Authentication failed: {errorDescription ?? error}"), "text/html");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Content(BuildCallbackHtml(false, "Invalid callback parameters."), "text/html");
        }

        // Parse state: "botId|pluginId"
        var parts = state.Split('|', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var botId))
        {
            return Content(BuildCallbackHtml(false, "Invalid state parameter."), "text/html");
        }
        var pluginId = parts[1];

        var pluginSettings = await GetPluginSettingsAsync(botId, pluginId, ct);
        pluginSettings.TryGetValue("ClientId", out var clientId);
        pluginSettings.TryGetValue("ClientSecret", out var clientSecret);
        pluginSettings.TryGetValue("TenantId", out var tenantId);
        if (string.IsNullOrEmpty(tenantId)) tenantId = "common";
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/plugins/oauth/callback";

        // Exchange code for tokens
        using var httpClient = new HttpClient();
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = clientId!,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["scope"] = "offline_access Mail.Read Mail.Send User.Read"
        };
        if (!string.IsNullOrEmpty(clientSecret))
            tokenRequest["client_secret"] = clientSecret;

        var response = await httpClient.PostAsync(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(tokenRequest), ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return Content(BuildCallbackHtml(false, $"Token exchange failed: {errorBody}"), "text/html");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var accessToken = json.GetProperty("access_token").GetString()!;
        var refreshToken = json.GetProperty("refresh_token").GetString()!;
        var expiresIn = json.GetProperty("expires_in").GetInt32();
        var tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        // Get user info from Graph
        var userEmail = "Unknown";
        try
        {
            using var graphClient = new HttpClient();
            graphClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var meResponse = await graphClient.GetAsync("https://graph.microsoft.com/v1.0/me", ct);
            if (meResponse.IsSuccessStatusCode)
            {
                var meJson = await meResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
                userEmail = meJson.TryGetProperty("mail", out var mail) && mail.ValueKind == JsonValueKind.String
                    ? mail.GetString()!
                    : meJson.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString()! : "Unknown";
            }
        }
        catch { /* non-critical */ }

        // Store tokens in the plugin config
        var pluginConfigs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == pluginId, ct);

        var pluginConfig = pluginConfigs.FirstOrDefault();
        if (pluginConfig == null)
        {
            pluginConfig = new PluginConfig
            {
                Id = Guid.NewGuid(),
                BotInstanceId = botId,
                PluginId = pluginId,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.PluginConfigs.AddAsync(pluginConfig, ct);
        }

        var settings = !string.IsNullOrEmpty(pluginConfig.Settings)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(pluginConfig.Settings) ?? new()
            : new Dictionary<string, string>();

        settings["AccessToken"] = accessToken;
        settings["RefreshToken"] = refreshToken;
        settings["TokenExpiry"] = tokenExpiry.ToString("O");
        settings["ConnectedEmail"] = userEmail;
        settings["ConnectedAt"] = DateTime.UtcNow.ToString("O");

        pluginConfig.Settings = JsonSerializer.Serialize(settings);
        pluginConfig.IsEnabled = true;
        pluginConfig.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PluginConfigs.UpdateAsync(pluginConfig, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Content(BuildCallbackHtml(true, $"Connected as {userEmail}"), "text/html");
    }

    [HttpGet("oauth/status/{botId}/{pluginId}")]
    public async Task<ActionResult> GetOAuthStatus(Guid botId, string pluginId, CancellationToken ct)
    {
        var pluginConfigs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == pluginId, ct);

        var pluginConfig = pluginConfigs.FirstOrDefault();
        if (pluginConfig == null || string.IsNullOrEmpty(pluginConfig.Settings))
        {
            return Ok(new { connected = false });
        }

        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(pluginConfig.Settings);
        if (settings == null || !settings.ContainsKey("AccessToken"))
        {
            return Ok(new { connected = false });
        }

        settings.TryGetValue("ConnectedEmail", out var email);
        settings.TryGetValue("ConnectedAt", out var connectedAt);

        return Ok(new
        {
            connected = true,
            email,
            connectedAt
        });
    }

    [HttpPost("oauth/disconnect/{botId}/{pluginId}")]
    public async Task<ActionResult> OAuthDisconnect(Guid botId, string pluginId, CancellationToken ct)
    {
        var pluginConfigs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == pluginId, ct);

        var pluginConfig = pluginConfigs.FirstOrDefault();
        if (pluginConfig == null || string.IsNullOrEmpty(pluginConfig.Settings))
        {
            return Ok(new { success = true });
        }

        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(pluginConfig.Settings) ?? new();

        settings.Remove("AccessToken");
        settings.Remove("RefreshToken");
        settings.Remove("TokenExpiry");
        settings.Remove("ConnectedEmail");
        settings.Remove("ConnectedAt");

        pluginConfig.Settings = JsonSerializer.Serialize(settings);
        pluginConfig.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PluginConfigs.UpdateAsync(pluginConfig, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { success = true });
    }

    private static string BuildCallbackHtml(bool success, string message)
    {
        var color = success ? "#16a34a" : "#dc2626";
        var icon = success ? "&#10004;" : "&#10006;";
        var title = success ? "Connected!" : "Connection Failed";
        var successJs = success ? "true" : "false";

        return "<!DOCTYPE html><html><head><title>OAuth Connection</title></head>"
            + "<body style=\"font-family: system-ui, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f9fafb;\">"
            + "<div style=\"text-align: center; padding: 2rem; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); max-width: 400px;\">"
            + $"<div style=\"font-size: 48px; color: {color}; margin-bottom: 16px;\">{icon}</div>"
            + $"<h2 style=\"margin: 0 0 8px; color: #111827;\">{title}</h2>"
            + $"<p style=\"color: #6b7280; margin: 0 0 24px;\">{message}</p>"
            + "<p style=\"color: #9ca3af; font-size: 14px;\">This window will close automatically...</p>"
            + "</div>"
            + "<script>"
            + $"if(window.opener){{window.opener.postMessage({{type:'plugin-oauth-complete',success:{successJs}}},'*');}}"
            + "setTimeout(function(){window.close();},2000);"
            + "</script></body></html>";
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
