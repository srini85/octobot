using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using OctoBot.Plugins.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace OctoBot.Plugins.Office365;

public class Office365EmailPlugin : IPlugin, IConfigurablePlugin
{
    private readonly Office365EmailFunctions _functions;
    private IPublicClientApplication? _msalApp;
    private string? _tenantId;
    private string? _clientId;
    private string? _telegramBotToken;
    private string? _telegramChatId;
    private int _maxEmailsPerCheck = 10;
    private DateTime _lastCheckTime = DateTime.UtcNow;
    private readonly string _tokenCacheDir;

    public Office365EmailPlugin()
    {
        _functions = new Office365EmailFunctions(this);
        _tokenCacheDir = Path.Combine(AppContext.BaseDirectory, "data", "tokens");
    }

    public PluginMetadata Metadata => new(
        Id: "office365-email",
        Name: "Office 365 Email",
        Description: "Monitors Office 365 email and sends new email summaries to Telegram",
        Version: "1.0.0",
        Author: "OctoBot",
        Settings: new[]
        {
            new PluginSettingDefinition(
                Key: "TenantId",
                DisplayName: "Tenant ID",
                Description: "Azure AD tenant ID (or 'common' for multi-tenant)",
                Type: PluginSettingType.String,
                IsRequired: true,
                DefaultValue: "common"
            ),
            new PluginSettingDefinition(
                Key: "ClientId",
                DisplayName: "Client ID",
                Description: "Azure AD app registration client ID",
                Type: PluginSettingType.String,
                IsRequired: true
            ),
            new PluginSettingDefinition(
                Key: "TelegramBotToken",
                DisplayName: "Telegram Bot Token",
                Description: "Bot token for sending email notifications to Telegram",
                Type: PluginSettingType.Secret,
                IsRequired: true
            ),
            new PluginSettingDefinition(
                Key: "TelegramChatId",
                DisplayName: "Telegram Chat ID",
                Description: "Target Telegram chat ID to receive email notifications",
                Type: PluginSettingType.String,
                IsRequired: true
            ),
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
        yield return AIFunctionFactory.Create(_functions.Authenticate, name: "Office365Email_Authenticate");
        yield return AIFunctionFactory.Create(_functions.CheckNewEmails, name: "Office365Email_CheckNewEmails");
    }

    public Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_tokenCacheDir);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public void Configure(Dictionary<string, string> settings)
    {
        if (settings.TryGetValue("TenantId", out var tenantId))
            _tenantId = tenantId;
        if (settings.TryGetValue("ClientId", out var clientId))
            _clientId = clientId;
        if (settings.TryGetValue("TelegramBotToken", out var token))
            _telegramBotToken = token;
        if (settings.TryGetValue("TelegramChatId", out var chatId))
            _telegramChatId = chatId;
        if (settings.TryGetValue("MaxEmailsPerCheck", out var maxEmails) && int.TryParse(maxEmails, out var max))
            _maxEmailsPerCheck = max;

        if (!string.IsNullOrEmpty(_tenantId) && !string.IsNullOrEmpty(_clientId))
        {
            _msalApp = PublicClientApplicationBuilder
                .Create(_clientId)
                .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
                .WithDefaultRedirectUri()
                .Build();

            EnableTokenCache(_msalApp.UserTokenCache);
        }
    }

    private void EnableTokenCache(ITokenCache tokenCache)
    {
        var cacheFilePath = Path.Combine(_tokenCacheDir, $"{_clientId}.bin");

        tokenCache.SetBeforeAccess(args =>
        {
            if (File.Exists(cacheFilePath))
            {
                args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(cacheFilePath));
            }
        });

        tokenCache.SetAfterAccess(args =>
        {
            if (args.HasStateChanged)
            {
                File.WriteAllBytes(cacheFilePath, args.TokenCache.SerializeMsalV3());
            }
        });
    }

    internal async Task<string> AuthenticateAsync()
    {
        if (_msalApp == null)
        {
            return "Plugin not configured. Please set TenantId and ClientId in plugin settings.";
        }

        var scopes = new[] { "Mail.Read", "User.Read" };

        // Check if we already have a cached token
        var accounts = await _msalApp.GetAccountsAsync();
        if (accounts.Any())
        {
            try
            {
                await _msalApp.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
                return "Already authenticated. Token is valid.";
            }
            catch (MsalUiRequiredException)
            {
                // Need to re-authenticate
            }
        }

        // Initiate device code flow
        try
        {
            var result = await _msalApp.AcquireTokenWithDeviceCode(scopes, callback =>
            {
                // The callback message contains the URL and code for the user
                return Task.CompletedTask;
            }).ExecuteAsync();

            return $"Authentication successful! Signed in as {result.Account.Username}.";
        }
        catch (MsalServiceException ex) when (ex.ErrorCode == "authorization_pending")
        {
            return "Authentication is pending. Please complete the sign-in in your browser.";
        }
        catch (OperationCanceledException)
        {
            return "Authentication was cancelled or timed out.";
        }
        catch (Exception ex)
        {
            return $"Authentication failed: {ex.Message}";
        }
    }

    internal async Task<string> StartDeviceCodeFlowAsync()
    {
        if (_msalApp == null)
        {
            return "Plugin not configured. Please set TenantId and ClientId in plugin settings.";
        }

        var scopes = new[] { "Mail.Read", "User.Read" };

        // Check if we already have a valid token
        var accounts = await _msalApp.GetAccountsAsync();
        if (accounts.Any())
        {
            try
            {
                await _msalApp.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
                return "Already authenticated with a valid token. No action needed.";
            }
            catch (MsalUiRequiredException)
            {
                // Need to re-authenticate
            }
        }

        string deviceCodeMessage = "";

        try
        {
            var result = await _msalApp.AcquireTokenWithDeviceCode(scopes, callback =>
            {
                deviceCodeMessage = callback.Message;
                return Task.CompletedTask;
            }).ExecuteAsync();

            return $"Authentication successful! Signed in as {result.Account.Username}.";
        }
        catch (MsalServiceException ex) when (ex.ErrorCode == "authorization_pending")
        {
            return !string.IsNullOrEmpty(deviceCodeMessage)
                ? deviceCodeMessage
                : "Authentication is pending. Please check the device code flow.";
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(deviceCodeMessage))
            {
                return $"{deviceCodeMessage}\n\n(Note: Authentication is in progress. Complete the sign-in in your browser.)";
            }
            return $"Authentication failed: {ex.Message}";
        }
    }

    private async Task<GraphServiceClient?> GetGraphClientAsync()
    {
        if (_msalApp == null) return null;

        var scopes = new[] { "Mail.Read", "User.Read" };
        var accounts = await _msalApp.GetAccountsAsync();

        if (!accounts.Any()) return null;

        try
        {
            var authResult = await _msalApp.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                .ExecuteAsync();

            var authProvider = new BaseBearerTokenAuthenticationProvider(
                new TokenProvider(authResult.AccessToken));

            return new GraphServiceClient(authProvider);
        }
        catch (MsalUiRequiredException)
        {
            return null;
        }
    }

    internal async Task<string> CheckNewEmailsAsync()
    {
        if (string.IsNullOrEmpty(_telegramBotToken) || string.IsNullOrEmpty(_telegramChatId))
        {
            return "Telegram settings not configured. Please set TelegramBotToken and TelegramChatId.";
        }

        var graphClient = await GetGraphClientAsync();
        if (graphClient == null)
        {
            return "Not authenticated. Please run Office365Email_Authenticate first.";
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

            if (messages?.Value == null || messages.Value.Count == 0)
            {
                return "No new unread emails found.";
            }

            var telegramBot = new TelegramBotClient(_telegramBotToken);
            var chatId = long.Parse(_telegramChatId);
            var emailCount = 0;

            foreach (var email in messages.Value)
            {
                var senderName = email.From?.EmailAddress?.Name ?? "Unknown";
                var senderEmail = email.From?.EmailAddress?.Address ?? "Unknown";
                var subject = email.Subject ?? "(No Subject)";
                var preview = email.BodyPreview ?? "";

                // Truncate preview to 200 chars
                if (preview.Length > 200)
                    preview = preview[..200] + "...";

                var notification = new StringBuilder();
                notification.AppendLine("\ud83d\udce7 <b>New Email</b>");
                notification.AppendLine($"<b>From:</b> {EscapeHtml(senderName)} &lt;{EscapeHtml(senderEmail)}&gt;");
                notification.AppendLine($"<b>Subject:</b> {EscapeHtml(subject)}");
                notification.AppendLine($"<b>Received:</b> {email.ReceivedDateTime?.ToString("g") ?? "Unknown"}");
                notification.AppendLine($"<b>Preview:</b> {EscapeHtml(preview)}");

                await telegramBot.SendMessage(
                    chatId: chatId,
                    text: notification.ToString(),
                    parseMode: ParseMode.Html
                );

                emailCount++;
            }

            return $"Found and notified about {emailCount} new email(s) via Telegram.";
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

    [Description("Authenticate with Office 365 using device code flow. Returns instructions for the user to complete sign-in.")]
    public async Task<string> Authenticate()
    {
        return await _plugin.StartDeviceCodeFlowAsync();
    }

    [Description("Check for new unread emails in Office 365 and send summaries to the configured Telegram chat.")]
    public async Task<string> CheckNewEmails()
    {
        return await _plugin.CheckNewEmailsAsync();
    }
}

/// <summary>
/// Simple token provider that wraps a pre-acquired access token for the Graph SDK.
/// </summary>
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
