using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using OctoBot.Core.Interfaces;
using OctoBot.Plugins.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace OctoBot.Plugins.Office365;

public class Office365EmailPlugin : IPlugin, IConfigurablePlugin
{
    private readonly Office365EmailFunctions _functions;
    private Guid _botInstanceId;
    private IServiceScopeFactory? _scopeFactory;
    private int _maxEmailsPerCheck = 10;
    private DateTime _lastCheckTime = DateTime.UtcNow;

    // OAuth tokens stored in plugin config by the auth controller
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public Office365EmailPlugin()
    {
        _functions = new Office365EmailFunctions(this);
    }

    public PluginMetadata Metadata => new(
        Id: "office365-email",
        Name: "Office 365 Email",
        Description: "Monitors Office 365 email and sends new email summaries to Telegram. Connect your Office 365 account using the button below, then create a scheduled job to check periodically.",
        Version: "1.0.0",
        Author: "OctoBot",
        Settings: new[]
        {
            new PluginSettingDefinition(
                Key: "MaxEmailsPerCheck",
                DisplayName: "Max Emails Per Check",
                Description: "Maximum number of emails to process per check (default: 10)",
                Type: PluginSettingType.Number,
                IsRequired: false,
                DefaultValue: "10"
            )
        }
    );

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(_functions.CheckNewEmails, name: "Office365Email_CheckNewEmails");
    }

    public Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public void Configure(Guid botInstanceId, Dictionary<string, string> settings, IServiceScopeFactory scopeFactory)
    {
        _botInstanceId = botInstanceId;
        _scopeFactory = scopeFactory;

        if (settings.TryGetValue("MaxEmailsPerCheck", out var maxEmails) && int.TryParse(maxEmails, out var max))
            _maxEmailsPerCheck = max;

        // Load OAuth tokens stored by the auth controller
        if (settings.TryGetValue("AccessToken", out var at))
            _accessToken = at;
        if (settings.TryGetValue("RefreshToken", out var rt))
            _refreshToken = rt;
        if (settings.TryGetValue("TokenExpiry", out var expiry) && DateTime.TryParse(expiry, out var exp))
            _tokenExpiry = exp;
        if (settings.TryGetValue("LastCheckTime", out var lct) && DateTime.TryParse(lct, out var lastCheck))
            _lastCheckTime = lastCheck;
    }

    private async Task<string?> GetTelegramBotTokenAsync()
    {
        if (_scopeFactory == null) return null;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var channelConfigs = await unitOfWork.ChannelConfigs.FindAsync(
            c => c.BotInstanceId == _botInstanceId && c.ChannelType == "telegram");

        var config = channelConfigs.FirstOrDefault();
        if (config == null || string.IsNullOrEmpty(config.Settings)) return null;

        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(config.Settings);
        string? botToken = null;
        settings?.TryGetValue("BotToken", out botToken);
        return botToken;
    }

    private async Task<List<long>> GetTelegramChatIdsAsync()
    {
        if (_scopeFactory == null) return new List<long>();

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var conversations = await unitOfWork.Conversations.GetByBotIdAsync(_botInstanceId, take: 100);

        var chatIds = new List<long>();
        foreach (var conv in conversations)
        {
            // Telegram chat IDs are numeric; filter out non-Telegram channels like "job-xxx"
            if (long.TryParse(conv.ChannelId, out var chatId))
            {
                chatIds.Add(chatId);
            }
        }

        return chatIds.Distinct().ToList();
    }

    private async Task<string?> EnsureValidAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return null;

        // If token is still valid, use it
        if (_tokenExpiry > DateTime.UtcNow.AddMinutes(5))
            return _accessToken;

        // Try to refresh
        if (string.IsNullOrEmpty(_refreshToken))
            return null;

        // Load Office365 config to get ClientId/ClientSecret for refresh
        if (_scopeFactory == null) return null;

        using var scope = _scopeFactory.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var clientId = config["Office365:ClientId"];
        var clientSecret = config["Office365:ClientSecret"];
        var tenantId = config["Office365:TenantId"] ?? "common";

        if (string.IsNullOrEmpty(clientId)) return null;

        using var httpClient = new HttpClient();
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken,
            ["scope"] = "offline_access Mail.Read User.Read"
        };
        if (!string.IsNullOrEmpty(clientSecret))
            tokenRequest["client_secret"] = clientSecret;

        var response = await httpClient.PostAsync(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(tokenRequest));

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        _accessToken = json.GetProperty("access_token").GetString();
        _refreshToken = json.GetProperty("refresh_token").GetString();
        var expiresIn = json.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        // Persist the refreshed tokens
        await UpdatePluginSettingsAsync(new Dictionary<string, string>
        {
            ["AccessToken"] = _accessToken!,
            ["RefreshToken"] = _refreshToken!,
            ["TokenExpiry"] = _tokenExpiry.ToString("O")
        });

        return _accessToken;
    }

    private async Task UpdatePluginSettingsAsync(Dictionary<string, string> updates)
    {
        if (_scopeFactory == null) return;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pluginConfigs = await unitOfWork.PluginConfigs.FindAsync(
            p => p.BotInstanceId == _botInstanceId && p.PluginId == "office365-email");

        var pluginConfig = pluginConfigs.FirstOrDefault();
        if (pluginConfig == null) return;

        var settings = !string.IsNullOrEmpty(pluginConfig.Settings)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(pluginConfig.Settings) ?? new()
            : new Dictionary<string, string>();

        foreach (var kv in updates)
            settings[kv.Key] = kv.Value;

        pluginConfig.Settings = JsonSerializer.Serialize(settings);
        pluginConfig.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.PluginConfigs.UpdateAsync(pluginConfig);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<GraphServiceClient?> GetGraphClientAsync()
    {
        var accessToken = await EnsureValidAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken)) return null;

        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new TokenProvider(accessToken));

        return new GraphServiceClient(authProvider);
    }

    internal async Task<string> CheckNewEmailsAsync()
    {
        var graphClient = await GetGraphClientAsync();
        if (graphClient == null)
        {
            return "Not connected to Office 365. Please connect your account from the plugin settings page.";
        }

        // Get Telegram bot token from channel config
        var botToken = await GetTelegramBotTokenAsync();
        if (string.IsNullOrEmpty(botToken))
        {
            return "Telegram channel is not configured for this bot. Please set up the Telegram channel first.";
        }

        // Get chat IDs from existing conversations
        var chatIds = await GetTelegramChatIdsAsync();
        if (chatIds.Count == 0)
        {
            return "No Telegram conversations found. Please send a message to the bot on Telegram first so it knows where to send notifications.";
        }

        try
        {
            var filterTime = _lastCheckTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var messages = await graphClient.Me.Messages.GetAsync(config =>
            {
                config.QueryParameters.Filter = $"isRead eq false and receivedDateTime ge {filterTime}";
                config.QueryParameters.Top = _maxEmailsPerCheck;
                config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime", "bodyPreview", "isRead" };
                config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
            });

            _lastCheckTime = DateTime.UtcNow;

            // Persist the last check time
            await UpdatePluginSettingsAsync(new Dictionary<string, string>
            {
                ["LastCheckTime"] = _lastCheckTime.ToString("O")
            });

            if (messages?.Value == null || messages.Value.Count == 0)
            {
                return "No new unread emails found.";
            }

            var telegramBot = new TelegramBotClient(botToken);
            var emailCount = 0;

            foreach (var email in messages.Value)
            {
                var senderName = email.From?.EmailAddress?.Name ?? "Unknown";
                var senderEmail = email.From?.EmailAddress?.Address ?? "Unknown";
                var subject = email.Subject ?? "(No Subject)";
                var preview = email.BodyPreview ?? "";

                if (preview.Length > 200)
                    preview = preview[..200] + "...";

                var notification = new StringBuilder();
                notification.AppendLine("\ud83d\udce7 <b>New Email</b>");
                notification.AppendLine($"<b>From:</b> {EscapeHtml(senderName)} &lt;{EscapeHtml(senderEmail)}&gt;");
                notification.AppendLine($"<b>Subject:</b> {EscapeHtml(subject)}");
                notification.AppendLine($"<b>Received:</b> {email.ReceivedDateTime?.ToString("g") ?? "Unknown"}");
                notification.AppendLine($"<b>Preview:</b> {EscapeHtml(preview)}");

                // Send to all known Telegram chats
                foreach (var chatId in chatIds)
                {
                    try
                    {
                        await telegramBot.SendMessage(
                            chatId: chatId,
                            text: notification.ToString(),
                            parseMode: ParseMode.Html
                        );
                    }
                    catch
                    {
                        // Skip chats that fail (e.g., user blocked bot)
                    }
                }

                emailCount++;
            }

            return $"Found and notified about {emailCount} new email(s) via Telegram to {chatIds.Count} chat(s).";
        }
        catch (Exception ex)
        {
            return $"Error checking emails: {ex.Message}";
        }
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}

public class Office365EmailFunctions
{
    private readonly Office365EmailPlugin _plugin;

    public Office365EmailFunctions(Office365EmailPlugin plugin)
    {
        _plugin = plugin;
    }

    [Description("Check for new unread emails in Office 365 and send summaries to Telegram.")]
    public async Task<string> CheckNewEmails()
    {
        return await _plugin.CheckNewEmailsAsync();
    }
}

internal class TokenProvider : IAccessTokenProvider
{
    private readonly string _accessToken;

    public TokenProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_accessToken);
    }
}
