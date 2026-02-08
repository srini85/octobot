using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/office365")]
public class Office365AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IUnitOfWork _unitOfWork;

    public Office365AuthController(IConfiguration config, IUnitOfWork unitOfWork)
    {
        _config = config;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Returns the OAuth authorization URL for the user to sign in with Microsoft.
    /// </summary>
    [HttpGet("auth-url")]
    public ActionResult GetAuthUrl([FromQuery] Guid botId)
    {
        var clientId = _config["Office365:ClientId"];
        var tenantId = _config["Office365:TenantId"] ?? "common";

        if (string.IsNullOrEmpty(clientId))
        {
            return BadRequest(new { error = "Office365 ClientId is not configured in appsettings.json" });
        }

        // Build redirect URI based on the current request
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/office365/callback";

        var authUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"
            + $"?client_id={Uri.EscapeDataString(clientId)}"
            + $"&response_type=code"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&scope={Uri.EscapeDataString("offline_access Mail.Read User.Read")}"
            + $"&state={botId}"
            + $"&response_mode=query";

        return Ok(new { authUrl, redirectUri });
    }

    /// <summary>
    /// OAuth callback - Microsoft redirects here after user signs in.
    /// Exchanges the authorization code for tokens and stores them in plugin config.
    /// </summary>
    [HttpGet("callback")]
    public async Task<ContentResult> Callback(
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

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state) || !Guid.TryParse(state, out var botId))
        {
            return Content(BuildCallbackHtml(false, "Invalid callback parameters."), "text/html");
        }

        var clientId = _config["Office365:ClientId"];
        var clientSecret = _config["Office365:ClientSecret"];
        var tenantId = _config["Office365:TenantId"] ?? "common";
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/office365/callback";

        // Exchange code for tokens
        using var httpClient = new HttpClient();
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = clientId!,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["scope"] = "offline_access Mail.Read User.Read"
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
            p => p.BotInstanceId == botId && p.PluginId == "office365-email", ct);

        var pluginConfig = pluginConfigs.FirstOrDefault();
        if (pluginConfig == null)
        {
            // Create plugin config if it doesn't exist
            pluginConfig = new PluginConfig
            {
                Id = Guid.NewGuid(),
                BotInstanceId = botId,
                PluginId = "office365-email",
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

    /// <summary>
    /// Returns the current connection status for Office 365 on a given bot.
    /// </summary>
    [HttpGet("status/{botId}")]
    public async Task<ActionResult> GetStatus(Guid botId, CancellationToken ct)
    {
        var pluginConfigs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == "office365-email", ct);

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

    /// <summary>
    /// Disconnects the Office 365 account by removing stored tokens.
    /// </summary>
    [HttpPost("disconnect/{botId}")]
    public async Task<ActionResult> Disconnect(Guid botId, CancellationToken ct)
    {
        var pluginConfigs = await _unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == botId && p.PluginId == "office365-email", ct);

        var pluginConfig = pluginConfigs.FirstOrDefault();
        if (pluginConfig == null || string.IsNullOrEmpty(pluginConfig.Settings))
        {
            return Ok(new { success = true });
        }

        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(pluginConfig.Settings) ?? new();

        // Remove OAuth tokens but keep other settings like MaxEmailsPerCheck
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

        return "<!DOCTYPE html><html><head><title>Office 365 Connection</title></head>"
            + "<body style=\"font-family: system-ui, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f9fafb;\">"
            + "<div style=\"text-align: center; padding: 2rem; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); max-width: 400px;\">"
            + $"<div style=\"font-size: 48px; color: {color}; margin-bottom: 16px;\">{icon}</div>"
            + $"<h2 style=\"margin: 0 0 8px; color: #111827;\">{title}</h2>"
            + $"<p style=\"color: #6b7280; margin: 0 0 24px;\">{message}</p>"
            + "<p style=\"color: #9ca3af; font-size: 14px;\">This window will close automatically...</p>"
            + "</div>"
            + "<script>"
            + $"if(window.opener){{window.opener.postMessage({{type:'office365-auth-complete',success:{successJs}}},'*');}}"
            + "setTimeout(function(){window.close();},2000);"
            + "</script></body></html>";
    }
}
