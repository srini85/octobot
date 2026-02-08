using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions.Authentication;
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

    // Auth mode: "IMAP" or "Microsoft Graph"
    private string _authMethod = "IMAP";

    // Shared settings
    private int _maxEmailsPerCheck = 10;
    private DateTime _lastCheckTime = DateTime.UtcNow;

    // IMAP mode
    private string? _email;
    private string? _password;
    private string _imapServer = "outlook.office365.com";
    private int _imapPort = 993;
    private bool _useSsl = true;
    private string _smtpServer = "smtp.office365.com";
    private int _smtpPort = 587;

    // Microsoft Graph mode (delegated OAuth)
    private string? _clientId;
    private string _tenantId = "common";
    private string? _clientSecret;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public Office365EmailPlugin()
    {
        _functions = new Office365EmailFunctions(this);
    }

    public PluginMetadata Metadata => new(
        Id: "office365-email",
        Name: "Email Monitor",
        Description: "Monitors an email inbox and sends new email summaries to Telegram. Supports IMAP (Gmail, Yahoo, personal Outlook) and Microsoft Graph with OAuth sign-in (Office 365 / Microsoft 365).",
        Version: "2.0.0",
        Author: "OctoBot",
        ReadMe: """
            ## Setup Instructions

            Choose the authentication method that matches your email provider.

            ---

            ### Mode 1: IMAP (Gmail, Yahoo, personal Outlook.com)

            Set **Auth Method** to **IMAP**. This uses traditional IMAP with a password or App Password.

            **Gmail:**
            1. Enable [2-Step Verification](https://myaccount.google.com/security) on your Google account.
            2. Go to [App Passwords](https://myaccount.google.com/apppasswords) and generate one for "Mail".
            3. Enter your Gmail address and the generated App Password.
            4. Set IMAP Server to `imap.gmail.com`, SMTP Server to `smtp.gmail.com`.

            **Yahoo Mail:**
            1. Enable [2-Step Verification](https://login.yahoo.com/account/security).
            2. Generate an App Password under "Other apps".
            3. Set IMAP Server to `imap.mail.yahoo.com`, SMTP Server to `smtp.mail.yahoo.com`.

            **Personal Outlook.com / Hotmail:**
            1. Go to [Microsoft Account Security](https://account.microsoft.com/security) and enable Two-Step Verification.
            2. Create an [App Password](https://account.microsoft.com/security/extra-security).
            3. IMAP Server: `outlook.office365.com` (default), SMTP Server: `smtp.office365.com` (default).

            ---

            ### Mode 2: Microsoft Graph (Office 365 / Microsoft 365)

            Set **Auth Method** to **Microsoft Graph**. You'll sign in with your Microsoft account to grant access — no passwords stored.

            **Step 1: Register an app in Azure Entra ID**
            1. Go to [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**.
            2. Name it (e.g. "OctoBot Email Monitor").
            3. Set **Supported account types** to match your needs (single tenant or multi-tenant).
            4. Set **Redirect URI** to **Web** with the value: `http://localhost:5000/api/plugins/oauth/callback` (adjust host/port to match your deployment).
            5. Click **Register**.
            6. Copy the **Application (client) ID** into the **Client ID** setting below.

            **Step 2: Create a client secret**
            1. Go to **Certificates & secrets** → **New client secret**.
            2. Copy the secret **Value** (not the Secret ID) into the **Client Secret** setting below.

            **Step 3: Add API permissions**
            1. Go to **API permissions** → **Add a permission** → **Microsoft Graph**.
            2. Choose **Delegated permissions**.
            3. Add: `Mail.Read`, `Mail.Send`, `User.Read`, `offline_access`.
            4. Click **Grant admin consent** if required by your organization.

            **Step 4: Save settings and connect**
            1. Save your Client ID and Client Secret settings below.
            2. Click the **Connect to Office 365** button to sign in and authorize access.

            ---

            After connecting, click **Test Connection** to verify.
            Then create a **Scheduled Job** (e.g. every 5 minutes: `*/5 * * * *`) with instructions like "Check for new emails and notify me".

            ---

            ### Replying to Emails

            You can ask the bot to reply to an email. It will:
            1. List your recent emails so you can pick one.
            2. Draft a reply for your review.
            3. Send it only after you confirm.
            """,
        Settings: new[]
        {
            new PluginSettingDefinition(
                Key: "AuthMethod",
                DisplayName: "Auth Method",
                Description: "IMAP for Gmail/Yahoo/personal Outlook; Microsoft Graph for Office 365 / Microsoft 365 (sign in with your Microsoft account)",
                Type: PluginSettingType.Select,
                IsRequired: true,
                DefaultValue: "IMAP",
                Options: new[] { "IMAP", "Microsoft Graph" }
            ),
            new PluginSettingDefinition(
                Key: "Email",
                DisplayName: "Email Address",
                Description: "For IMAP mode: your email address",
                Type: PluginSettingType.String,
                IsRequired: false
            ),
            new PluginSettingDefinition(
                Key: "Password",
                DisplayName: "Password / App Password",
                Description: "For IMAP mode: your email password or App Password",
                Type: PluginSettingType.Secret,
                IsRequired: false
            ),
            new PluginSettingDefinition(
                Key: "ImapServer",
                DisplayName: "IMAP Server",
                Description: "For IMAP mode: server hostname (e.g. outlook.office365.com, imap.gmail.com)",
                Type: PluginSettingType.String,
                IsRequired: false,
                DefaultValue: "outlook.office365.com"
            ),
            new PluginSettingDefinition(
                Key: "ImapPort",
                DisplayName: "IMAP Port",
                Description: "For IMAP mode: server port (usually 993 for SSL)",
                Type: PluginSettingType.Number,
                IsRequired: false,
                DefaultValue: "993"
            ),
            new PluginSettingDefinition(
                Key: "UseSsl",
                DisplayName: "Use SSL",
                Description: "For IMAP mode: connect using SSL/TLS (recommended)",
                Type: PluginSettingType.Boolean,
                IsRequired: false,
                DefaultValue: "true"
            ),
            new PluginSettingDefinition(
                Key: "SmtpServer",
                DisplayName: "SMTP Server",
                Description: "For IMAP mode: SMTP server for sending replies (e.g. smtp.office365.com, smtp.gmail.com)",
                Type: PluginSettingType.String,
                IsRequired: false,
                DefaultValue: "smtp.office365.com"
            ),
            new PluginSettingDefinition(
                Key: "SmtpPort",
                DisplayName: "SMTP Port",
                Description: "For IMAP mode: SMTP server port (usually 587 for STARTTLS)",
                Type: PluginSettingType.Number,
                IsRequired: false,
                DefaultValue: "587"
            ),
            new PluginSettingDefinition(
                Key: "ClientId",
                DisplayName: "Client ID",
                Description: "For Microsoft Graph mode: Azure AD Application (client) ID",
                Type: PluginSettingType.String,
                IsRequired: false
            ),
            new PluginSettingDefinition(
                Key: "TenantId",
                DisplayName: "Tenant ID",
                Description: "For Microsoft Graph mode: Azure AD Directory (tenant) ID, or 'common' for multi-tenant",
                Type: PluginSettingType.String,
                IsRequired: false,
                DefaultValue: "common"
            ),
            new PluginSettingDefinition(
                Key: "ClientSecret",
                DisplayName: "Client Secret",
                Description: "For Microsoft Graph mode: Azure AD client secret value",
                Type: PluginSettingType.Secret,
                IsRequired: false
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
        yield return AIFunctionFactory.Create(_functions.GetRecentEmails, name: "Office365Email_GetRecentEmails");
        yield return AIFunctionFactory.Create(_functions.ReplyToEmail, name: "Office365Email_ReplyToEmail");
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

        // IMAP mode
        if (settings.TryGetValue("Email", out var email))
            _email = email;
        if (settings.TryGetValue("Password", out var password))
            _password = password;
        if (settings.TryGetValue("ImapServer", out var server) && !string.IsNullOrEmpty(server))
            _imapServer = server;
        if (settings.TryGetValue("ImapPort", out var port) && int.TryParse(port, out var p))
            _imapPort = p;
        if (settings.TryGetValue("UseSsl", out var ssl))
            _useSsl = ssl.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (settings.TryGetValue("SmtpServer", out var smtpServer) && !string.IsNullOrEmpty(smtpServer))
            _smtpServer = smtpServer;
        if (settings.TryGetValue("SmtpPort", out var smtpPort) && int.TryParse(smtpPort, out var sp))
            _smtpPort = sp;

        // Microsoft Graph mode
        if (settings.TryGetValue("ClientId", out var clientId))
            _clientId = clientId;
        if (settings.TryGetValue("TenantId", out var tenantId) && !string.IsNullOrEmpty(tenantId))
            _tenantId = tenantId;
        if (settings.TryGetValue("ClientSecret", out var clientSecret))
            _clientSecret = clientSecret;

        // OAuth tokens (stored by auth controller)
        if (settings.TryGetValue("AccessToken", out var at))
            _accessToken = at;
        if (settings.TryGetValue("RefreshToken", out var rt))
            _refreshToken = rt;
        if (settings.TryGetValue("TokenExpiry", out var expiry) && DateTime.TryParse(expiry, out var exp))
            _tokenExpiry = exp;

        // Shared
        if (settings.TryGetValue("MaxEmailsPerCheck", out var maxEmails) && int.TryParse(maxEmails, out var max))
            _maxEmailsPerCheck = max;
        if (settings.TryGetValue("LastCheckTime", out var lct) && DateTime.TryParse(lct, out var lastCheck))
            _lastCheckTime = lastCheck;
    }

    private bool IsGraphMode => _authMethod.Equals("Microsoft Graph", StringComparison.OrdinalIgnoreCase);

    private string? ValidateConfig()
    {
        if (IsGraphMode)
        {
            if (string.IsNullOrEmpty(_accessToken))
                return "Not connected to Office 365. Please click 'Connect to Office 365' to sign in.";
        }
        else
        {
            if (string.IsNullOrEmpty(_email))
                return "Email address is required for IMAP mode.";
            if (string.IsNullOrEmpty(_password))
                return "Password is required for IMAP mode.";
        }

        return null;
    }

    private async Task<string?> EnsureValidAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return null;

        // If token is still valid, use it
        if (_tokenExpiry > DateTime.UtcNow.AddMinutes(5))
            return _accessToken;

        // Try to refresh
        if (string.IsNullOrEmpty(_refreshToken) || string.IsNullOrEmpty(_clientId))
            return null;

        using var httpClient = new HttpClient();
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken,
            ["scope"] = "offline_access Mail.Read Mail.Send User.Read"
        };
        if (!string.IsNullOrEmpty(_clientSecret))
            tokenRequest["client_secret"] = _clientSecret;

        var response = await httpClient.PostAsync(
            $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token",
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

    private async Task<GraphServiceClient?> GetGraphClientAsync()
    {
        var accessToken = await EnsureValidAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken)) return null;

        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new TokenProvider(accessToken));

        return new GraphServiceClient(authProvider);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        var error = ValidateConfig();
        if (error != null)
            return (false, error);

        try
        {
            if (IsGraphMode)
            {
                var graphClient = await GetGraphClientAsync();
                if (graphClient == null)
                    return (false, "Not connected to Office 365. Please click 'Connect to Office 365' to sign in.");

                var inbox = await graphClient.Me.MailFolders["Inbox"].GetAsync();
                var count = inbox?.TotalItemCount ?? 0;

                return (true, $"Connected successfully via Microsoft Graph! Inbox has {count} message(s).");
            }
            else
            {
                using var client = new ImapClient();
                await client.ConnectAsync(_imapServer, _imapPort, _useSsl);
                await client.AuthenticateAsync(_email, _password);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);
                var count = inbox.Count;

                await client.DisconnectAsync(true);

                return (true, $"Connected successfully via IMAP! Inbox has {count} message(s).");
            }
        }
        catch (ODataError odataError)
        {
            var code = odataError.Error?.Code ?? "Unknown";
            var msg = odataError.Error?.Message ?? odataError.Message;
            return (false, $"Graph API error ({code}): {msg}");
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

    internal async Task UpdatePluginSettingsAsync(Dictionary<string, string> updates)
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
            if (IsGraphMode)
                return await CheckNewEmailsViaGraphAsync(botToken, chatIds);
            else
                return await CheckNewEmailsViaImapAsync(botToken, chatIds);
        }
        catch (ODataError odataError)
        {
            var code = odataError.Error?.Code ?? "Unknown";
            var msg = odataError.Error?.Message ?? odataError.Message;
            return $"Graph API error ({code}): {msg}";
        }
        catch (Exception ex)
        {
            return $"Error checking emails: {ex.Message}";
        }
    }

    internal async Task<string> GetRecentEmailsAsync(int count = 10)
    {
        var error = ValidateConfig();
        if (error != null)
            return $"Email plugin is not configured: {error}";

        try
        {
            if (IsGraphMode)
                return await GetRecentEmailsViaGraphAsync(count);
            else
                return await GetRecentEmailsViaImapAsync(count);
        }
        catch (ODataError odataError)
        {
            var code = odataError.Error?.Code ?? "Unknown";
            var msg = odataError.Error?.Message ?? odataError.Message;
            return $"Graph API error ({code}): {msg}";
        }
        catch (Exception ex)
        {
            return $"Error getting emails: {ex.Message}";
        }
    }

    private async Task<string> GetRecentEmailsViaGraphAsync(int count)
    {
        var graphClient = await GetGraphClientAsync();
        if (graphClient == null)
            return "Not connected to Office 365. Please reconnect from the plugin settings page.";

        var messages = await graphClient.Me.Messages.GetAsync(config =>
        {
            config.QueryParameters.Top = count;
            config.QueryParameters.Select = new[] { "id", "subject", "from", "receivedDateTime", "bodyPreview", "isRead" };
            config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
        });

        if (messages?.Value == null || messages.Value.Count == 0)
            return "No emails found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {messages.Value.Count} recent email(s):\n");

        foreach (var email in messages.Value)
        {
            var sender = email.From?.EmailAddress?.Name ?? email.From?.EmailAddress?.Address ?? "Unknown";
            var subject = email.Subject ?? "(No Subject)";
            var date = email.ReceivedDateTime?.ToString("g") ?? "Unknown";
            var readStatus = email.IsRead == true ? "Read" : "Unread";
            var preview = email.BodyPreview ?? "";
            if (preview.Length > 100) preview = preview[..100] + "...";

            sb.AppendLine($"- **ID:** {email.Id}");
            sb.AppendLine($"  **From:** {sender}");
            sb.AppendLine($"  **Subject:** {subject}");
            sb.AppendLine($"  **Date:** {date} ({readStatus})");
            sb.AppendLine($"  **Preview:** {preview}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> GetRecentEmailsViaImapAsync(int count)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_imapServer, _imapPort, _useSsl);
        await client.AuthenticateAsync(_email, _password);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        if (inbox.Count == 0)
        {
            await client.DisconnectAsync(true);
            return "No emails found.";
        }

        var startIndex = Math.Max(0, inbox.Count - count);
        var summaries = await inbox.FetchAsync(startIndex, -1,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags);

        var sb = new StringBuilder();
        sb.AppendLine($"Found {summaries.Count} recent email(s):\n");

        foreach (var summary in summaries.OrderByDescending(s => s.Date))
        {
            var sender = summary.Envelope.From?.Mailboxes?.FirstOrDefault()?.Name
                ?? summary.Envelope.From?.Mailboxes?.FirstOrDefault()?.Address ?? "Unknown";
            var subject = summary.Envelope.Subject ?? "(No Subject)";
            var date = summary.Date.LocalDateTime.ToString("g");
            var readStatus = summary.Flags?.HasFlag(MessageFlags.Seen) == true ? "Read" : "Unread";

            sb.AppendLine($"- **ID:** {summary.UniqueId}");
            sb.AppendLine($"  **From:** {sender}");
            sb.AppendLine($"  **Subject:** {subject}");
            sb.AppendLine($"  **Date:** {date} ({readStatus})");
            sb.AppendLine();
        }

        await client.DisconnectAsync(true);
        return sb.ToString();
    }

    internal async Task<string> ReplyToEmailAsync(string messageId, string replyBody)
    {
        var error = ValidateConfig();
        if (error != null)
            return $"Email plugin is not configured: {error}";

        try
        {
            if (IsGraphMode)
                return await ReplyToEmailViaGraphAsync(messageId, replyBody);
            else
                return await ReplyToEmailViaImapAsync(messageId, replyBody);
        }
        catch (ODataError odataError)
        {
            var code = odataError.Error?.Code ?? "Unknown";
            var msg = odataError.Error?.Message ?? odataError.Message;
            return $"Graph API error ({code}): {msg}";
        }
        catch (Exception ex)
        {
            return $"Error sending reply: {ex.Message}";
        }
    }

    private async Task<string> ReplyToEmailViaGraphAsync(string messageId, string replyBody)
    {
        var graphClient = await GetGraphClientAsync();
        if (graphClient == null)
            return "Not connected to Office 365. Please reconnect from the plugin settings page.";

        await graphClient.Me.Messages[messageId].Reply.PostAsync(
            new Microsoft.Graph.Me.Messages.Item.Reply.ReplyPostRequestBody
            {
                Comment = replyBody
            });

        return "Reply sent successfully.";
    }

    private async Task<string> ReplyToEmailViaImapAsync(string messageId, string replyBody)
    {
        if (!uint.TryParse(messageId, out var uid))
            return "Invalid message ID. Please use the ID from GetRecentEmails.";

        // Fetch the original message via IMAP
        using var imapClient = new ImapClient();
        await imapClient.ConnectAsync(_imapServer, _imapPort, _useSsl);
        await imapClient.AuthenticateAsync(_email, _password);

        var inbox = imapClient.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        var originalMessage = await inbox.GetMessageAsync(new UniqueId(uid));
        await imapClient.DisconnectAsync(true);

        if (originalMessage == null)
            return "Could not find the original email to reply to.";

        // Build the reply
        var reply = new MimeMessage();
        reply.From.Add(new MailboxAddress(_email, _email));

        // Reply to the sender
        if (originalMessage.ReplyTo.Count > 0)
            reply.To.AddRange(originalMessage.ReplyTo);
        else
            reply.To.AddRange(originalMessage.From);

        // Set subject
        var subject = originalMessage.Subject ?? "";
        reply.Subject = subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
            ? subject
            : $"Re: {subject}";

        // Set In-Reply-To and References headers
        if (!string.IsNullOrEmpty(originalMessage.MessageId))
        {
            reply.InReplyTo = originalMessage.MessageId;
            foreach (var reference in originalMessage.References)
                reply.References.Add(reference);
            reply.References.Add(originalMessage.MessageId);
        }

        // Build body with quoted original
        var originalSender = originalMessage.From?.Mailboxes?.FirstOrDefault()?.Address ?? "unknown";
        var originalDate = originalMessage.Date.LocalDateTime.ToString("f");
        var originalText = originalMessage.TextBody ?? "";

        var bodyText = $"{replyBody}\n\nOn {originalDate}, {originalSender} wrote:\n> {originalText.Replace("\n", "\n> ")}";
        reply.Body = new TextPart("plain") { Text = bodyText };

        // Send via SMTP
        using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
        await smtpClient.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await smtpClient.AuthenticateAsync(_email, _password);
        await smtpClient.SendAsync(reply);
        await smtpClient.DisconnectAsync(true);

        return "Reply sent successfully.";
    }

    private async Task<string> CheckNewEmailsViaGraphAsync(string botToken, List<long> chatIds)
    {
        var graphClient = await GetGraphClientAsync();
        if (graphClient == null)
        {
            return "Not connected to Office 365. Please reconnect from the plugin settings page.";
        }

        var filterTime = _lastCheckTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var messages = await graphClient.Me.Messages.GetAsync(config =>
        {
            config.QueryParameters.Filter = $"isRead eq false and receivedDateTime ge {filterTime}";
            config.QueryParameters.Top = _maxEmailsPerCheck;
            config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime", "bodyPreview", "isRead" };
            config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
        });

        _lastCheckTime = DateTime.UtcNow;
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
                    // Skip chats that fail
                }
            }

            emailCount++;
        }

        return $"Found and notified about {emailCount} new email(s) via Telegram to {chatIds.Count} chat(s).";
    }

    private async Task<string> CheckNewEmailsViaImapAsync(string botToken, List<long> chatIds)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_imapServer, _imapPort, _useSsl);
        await client.AuthenticateAsync(_email, _password);

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
                    // Skip chats that fail
                }
            }

            emailCount++;
        }

        await client.DisconnectAsync(true);

        return $"Found and notified about {emailCount} new email(s) via Telegram to {chatIds.Count} chat(s).";
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
