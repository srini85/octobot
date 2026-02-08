using System.ComponentModel;
using System.Text;
using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using OctoBot.Core.Interfaces;
using OctoBot.Plugins.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace OctoBot.Plugins.Office365;

public class Office365EmailPlugin : IPlugin, IConfigurablePlugin, ITestablePlugin
{
    private readonly Office365EmailFunctions _functions;
    private Guid _botInstanceId;
    private IServiceScopeFactory? _scopeFactory;

    // Auth mode: "Password" or "OAuth2"
    private string _authMethod = "Password";

    // Shared settings
    private string? _email;
    private string _imapServer = "outlook.office365.com";
    private int _imapPort = 993;
    private bool _useSsl = true;
    private int _maxEmailsPerCheck = 10;
    private DateTime _lastCheckTime = DateTime.UtcNow;

    // Password mode
    private string? _password;

    // OAuth2 mode (client credentials)
    private string? _clientId;
    private string? _tenantId;
    private string? _clientSecret;

    public Office365EmailPlugin()
    {
        _functions = new Office365EmailFunctions(this);
    }

    public PluginMetadata Metadata => new(
        Id: "office365-email",
        Name: "Email Monitor (IMAP)",
        Description: "Monitors an email inbox via IMAP and sends new email summaries to Telegram. Supports password auth (Gmail, Yahoo, personal Outlook) and OAuth2 (Office 365 business).",
        Version: "2.0.0",
        Author: "OctoBot",
        ReadMe: """
            ## Setup Instructions

            This plugin connects to your email via IMAP. Choose the authentication method that matches your email provider.

            ---

            ### Mode 1: Password / App Password (Gmail, Yahoo, personal Outlook.com)

            Set **Auth Method** to **Password**.

            **Gmail:**
            1. Enable [2-Step Verification](https://myaccount.google.com/security) on your Google account.
            2. Go to [App Passwords](https://myaccount.google.com/apppasswords) and generate one for "Mail".
            3. Enter your Gmail address and the generated App Password.
            4. Set IMAP Server to `imap.gmail.com`.

            **Yahoo Mail:**
            1. Enable [2-Step Verification](https://login.yahoo.com/account/security).
            2. Generate an App Password under "Other apps".
            3. Set IMAP Server to `imap.mail.yahoo.com`.

            **Personal Outlook.com / Hotmail:**
            1. Go to [Microsoft Account Security](https://account.microsoft.com/security) and enable Two-Step Verification.
            2. Create an [App Password](https://account.microsoft.com/security/extra-security).
            3. IMAP Server: `outlook.office365.com` (default).

            ---

            ### Mode 2: OAuth2 (Office 365 / Microsoft 365 Business)

            Set **Auth Method** to **OAuth2**. Basic auth / app passwords are disabled for business accounts — OAuth2 is required.

            **Step 1: Register an app in Azure Entra ID**
            1. Go to [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**.
            2. Name it (e.g. "OctoBot Email Monitor"), set **Supported account types** to "Accounts in this organizational directory only".
            3. No redirect URI needed. Click **Register**.
            4. Copy the **Application (client) ID** and **Directory (tenant) ID** into the settings below.

            **Step 2: Create a client secret**
            1. Go to **Certificates & secrets** → **New client secret**.
            2. Copy the secret **Value** (not the ID) into the **Client Secret** setting.

            **Step 3: Add API permissions**
            1. Go to **API permissions** → **Add a permission** → **APIs my organization uses**.
            2. Search for **Office 365 Exchange Online** and select it.
            3. Choose **Application permissions** → check **IMAP.AccessAsApp**.
            4. Click **Grant admin consent for [your org]**.

            **Step 4: Register the service principal in Exchange Online**
            Run the following in Exchange Online PowerShell:
            ```
            New-ServicePrincipal -AppId <CLIENT_ID> -ObjectId <ENTERPRISE_APP_OBJECT_ID>
            Add-MailboxPermission -Identity "user@yourdomain.com" -User <ENTERPRISE_APP_OBJECT_ID> -AccessRights FullAccess
            ```
            ⚠️ Use the **Object ID** from **Enterprise Applications** (not App registrations).

            ---

            After configuring, click **Test Connection** to verify.
            Then create a **Scheduled Job** (e.g. every 5 minutes: `*/5 * * * *`) with instructions like "Check for new emails and notify me".
            """,
        Settings: new[]
        {
            new PluginSettingDefinition(
                Key: "AuthMethod",
                DisplayName: "Auth Method",
                Description: "Password for Gmail/Yahoo/personal Outlook; OAuth2 for Office 365 business accounts",
                Type: PluginSettingType.Select,
                IsRequired: true,
                DefaultValue: "Password",
                Options: new[] { "Password", "OAuth2" }
            ),
            new PluginSettingDefinition(
                Key: "Email",
                DisplayName: "Email Address",
                Description: "Your email address (e.g. user@outlook.com, user@company.com)",
                Type: PluginSettingType.String,
                IsRequired: true
            ),
            new PluginSettingDefinition(
                Key: "Password",
                DisplayName: "Password / App Password",
                Description: "For Password mode: your email password or App Password",
                Type: PluginSettingType.Secret,
                IsRequired: false
            ),
            new PluginSettingDefinition(
                Key: "ClientId",
                DisplayName: "Client ID (OAuth2)",
                Description: "For OAuth2 mode: Azure AD Application (client) ID",
                Type: PluginSettingType.String,
                IsRequired: false
            ),
            new PluginSettingDefinition(
                Key: "TenantId",
                DisplayName: "Tenant ID (OAuth2)",
                Description: "For OAuth2 mode: Azure AD Directory (tenant) ID",
                Type: PluginSettingType.String,
                IsRequired: false
            ),
            new PluginSettingDefinition(
                Key: "ClientSecret",
                DisplayName: "Client Secret (OAuth2)",
                Description: "For OAuth2 mode: Azure AD client secret value",
                Type: PluginSettingType.Secret,
                IsRequired: false
            ),
            new PluginSettingDefinition(
                Key: "ImapServer",
                DisplayName: "IMAP Server",
                Description: "IMAP server hostname (e.g. outlook.office365.com, imap.gmail.com)",
                Type: PluginSettingType.String,
                IsRequired: false,
                DefaultValue: "outlook.office365.com"
            ),
            new PluginSettingDefinition(
                Key: "ImapPort",
                DisplayName: "IMAP Port",
                Description: "IMAP server port (usually 993 for SSL)",
                Type: PluginSettingType.Number,
                IsRequired: false,
                DefaultValue: "993"
            ),
            new PluginSettingDefinition(
                Key: "UseSsl",
                DisplayName: "Use SSL",
                Description: "Connect using SSL/TLS (recommended)",
                Type: PluginSettingType.Boolean,
                IsRequired: false,
                DefaultValue: "true"
            ),
            new PluginSettingDefinition(
                Key: "MaxEmailsPerCheck",
                DisplayName: "Max Emails Per Check",
                Description: "Maximum number of emails to process per check",
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

        if (settings.TryGetValue("AuthMethod", out var authMethod) && !string.IsNullOrEmpty(authMethod))
            _authMethod = authMethod;
        if (settings.TryGetValue("Email", out var email))
            _email = email;

        // Password mode
        if (settings.TryGetValue("Password", out var password))
            _password = password;

        // OAuth2 mode
        if (settings.TryGetValue("ClientId", out var clientId))
            _clientId = clientId;
        if (settings.TryGetValue("TenantId", out var tenantId))
            _tenantId = tenantId;
        if (settings.TryGetValue("ClientSecret", out var clientSecret))
            _clientSecret = clientSecret;

        // Shared
        if (settings.TryGetValue("ImapServer", out var server) && !string.IsNullOrEmpty(server))
            _imapServer = server;
        if (settings.TryGetValue("ImapPort", out var port) && int.TryParse(port, out var p))
            _imapPort = p;
        if (settings.TryGetValue("UseSsl", out var ssl))
            _useSsl = ssl.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (settings.TryGetValue("MaxEmailsPerCheck", out var maxEmails) && int.TryParse(maxEmails, out var max))
            _maxEmailsPerCheck = max;
        if (settings.TryGetValue("LastCheckTime", out var lct) && DateTime.TryParse(lct, out var lastCheck))
            _lastCheckTime = lastCheck;
    }

    private bool IsOAuth2 => _authMethod.Equals("OAuth2", StringComparison.OrdinalIgnoreCase);

    private string? ValidateConfig()
    {
        if (string.IsNullOrEmpty(_email))
            return "Email address is required.";

        if (IsOAuth2)
        {
            if (string.IsNullOrEmpty(_clientId))
                return "Client ID is required for OAuth2 mode.";
            if (string.IsNullOrEmpty(_tenantId))
                return "Tenant ID is required for OAuth2 mode.";
            if (string.IsNullOrEmpty(_clientSecret))
                return "Client Secret is required for OAuth2 mode.";
        }
        else
        {
            if (string.IsNullOrEmpty(_password))
                return "Password is required for Password mode.";
        }

        return null;
    }

    private async Task<string> AcquireOAuth2TokenAsync()
    {
        var app = ConfidentialClientApplicationBuilder.Create(_clientId)
            .WithAuthority($"https://login.microsoftonline.com/{_tenantId}/v2.0")
            .WithClientSecret(_clientSecret)
            .Build();

        var result = await app.AcquireTokenForClient(
            new[] { "https://outlook.office365.com/.default" }
        ).ExecuteAsync();

        return result.AccessToken;
    }

    private async Task AuthenticateImapClientAsync(ImapClient client)
    {
        await client.ConnectAsync(_imapServer, _imapPort, _useSsl);

        if (IsOAuth2)
        {
            var accessToken = await AcquireOAuth2TokenAsync();
            var oauth2 = new SaslMechanismOAuth2(_email, accessToken);
            await client.AuthenticateAsync(oauth2);
        }
        else
        {
            await client.AuthenticateAsync(_email, _password);
        }
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        var error = ValidateConfig();
        if (error != null)
            return (false, error);

        try
        {
            using var client = new ImapClient();
            await AuthenticateImapClientAsync(client);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);
            var count = inbox.Count;

            await client.DisconnectAsync(true);

            var mode = IsOAuth2 ? "OAuth2" : "Password";
            return (true, $"Connected successfully via {mode}! Inbox has {count} message(s).");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
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
            if (long.TryParse(conv.ChannelId, out var chatId))
            {
                chatIds.Add(chatId);
            }
        }

        return chatIds.Distinct().ToList();
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

    internal async Task<string> CheckNewEmailsAsync()
    {
        var error = ValidateConfig();
        if (error != null)
            return $"Email plugin is not configured: {error}";

        var botToken = await GetTelegramBotTokenAsync();
        if (string.IsNullOrEmpty(botToken))
        {
            return "Telegram channel is not configured for this bot. Please set up the Telegram channel first.";
        }

        var chatIds = await GetTelegramChatIdsAsync();
        if (chatIds.Count == 0)
        {
            return "No Telegram conversations found. Please send a message to the bot on Telegram first so it knows where to send notifications.";
        }

        try
        {
            using var client = new ImapClient();
            await AuthenticateImapClientAsync(client);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var query = SearchQuery.NotSeen.And(SearchQuery.DeliveredAfter(_lastCheckTime));
            var uids = await inbox.SearchAsync(query);

            _lastCheckTime = DateTime.UtcNow;
            await UpdatePluginSettingsAsync(new Dictionary<string, string>
            {
                ["LastCheckTime"] = _lastCheckTime.ToString("O")
            });

            if (uids.Count == 0)
            {
                await client.DisconnectAsync(true);
                return "No new unread emails found.";
            }

            var telegramBot = new TelegramBotClient(botToken);
            var emailCount = 0;

            var uidsToProcess = uids.Count > _maxEmailsPerCheck
                ? uids.Skip(uids.Count - _maxEmailsPerCheck).ToList()
                : uids;

            foreach (var uid in uidsToProcess)
            {
                var message = await inbox.GetMessageAsync(uid);

                var senderName = message.From?.Mailboxes?.FirstOrDefault()?.Name ?? "Unknown";
                var senderEmail = message.From?.Mailboxes?.FirstOrDefault()?.Address ?? "Unknown";
                var subject = message.Subject ?? "(No Subject)";
                var preview = message.TextBody ?? message.HtmlBody ?? "";

                if (preview.Length > 200)
                    preview = preview[..200] + "...";

                var notification = new StringBuilder();
                notification.AppendLine("\ud83d\udce7 <b>New Email</b>");
                notification.AppendLine($"<b>From:</b> {EscapeHtml(senderName)} &lt;{EscapeHtml(senderEmail)}&gt;");
                notification.AppendLine($"<b>Subject:</b> {EscapeHtml(subject)}");
                notification.AppendLine($"<b>Received:</b> {message.Date.LocalDateTime:g}");
                notification.AppendLine($"<b>Preview:</b> {EscapeHtml(preview)}");

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

            await client.DisconnectAsync(true);

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

    [Description("Check for new unread emails and send summaries to Telegram.")]
    public async Task<string> CheckNewEmails()
    {
        return await _plugin.CheckNewEmailsAsync();
    }
}
