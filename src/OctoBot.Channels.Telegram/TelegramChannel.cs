using OctoBot.Channels.Abstractions;
using OctoBot.Core.ValueObjects;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OctoBot.Channels.Telegram;

public class TelegramChannel : IChannel, IDisposable
{
    private readonly TelegramBotClient _client;
    private readonly ChannelConfiguration _config;
    private CancellationTokenSource? _cts;

    public string Name => "telegram";
    public string DisplayName => "Telegram";
    public bool IsConnected { get; private set; }
    public ChannelStatus Status { get; private set; } = ChannelStatus.Stopped;

    public event Func<IncomingMessage, Task>? OnMessageReceived;
    public event Func<ChannelError, Task>? OnError;

    public TelegramChannel(ChannelConfiguration config)
    {
        _config = config;
        var token = config.Settings.GetValueOrDefault("BotToken")
            ?? throw new ArgumentException("BotToken is required for Telegram channel");
        _client = new TelegramBotClient(token);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (Status == ChannelStatus.Connected) return;

        Status = ChannelStatus.Starting;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        _client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            _cts.Token
        );

        var me = await _client.GetMe(ct);
        IsConnected = true;
        Status = ChannelStatus.Connected;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        IsConnected = false;
        Status = ChannelStatus.Stopped;
        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(OutgoingMessage message, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var chatId = long.Parse(message.ChannelId);
        var parseMode = message.Format switch
        {
            MessageFormat.Markdown => ParseMode.MarkdownV2,
            MessageFormat.Html => ParseMode.Html,
            _ => ParseMode.None
        };

        if (message.Attachments?.Count > 0)
        {
            foreach (var attachment in message.Attachments)
            {
                using var stream = new MemoryStream(attachment.Data);
                await _client.SendDocument(
                    chatId: chatId,
                    document: InputFile.FromStream(stream, attachment.FileName),
                    caption: attachment.Caption,
                    cancellationToken: ct
                );
            }
        }

        if (!string.IsNullOrEmpty(message.Content))
        {
            var replyParams = message.ReplyToMessageId != null && int.TryParse(message.ReplyToMessageId, out var replyId)
                ? new ReplyParameters { MessageId = replyId }
                : null;

            await _client.SendMessage(
                chatId: chatId,
                text: message.Content,
                parseMode: parseMode,
                replyParameters: replyParams,
                cancellationToken: ct
            );
        }
    }

    public async Task<bool> ValidateConfigurationAsync(ChannelConfiguration config, CancellationToken ct = default)
    {
        var token = config.Settings.GetValueOrDefault("BotToken");
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            var client = new TelegramBotClient(token);
            await client.GetMe(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        if (update.Message?.Text == null) return;

        var message = update.Message;
        var attachments = new List<Attachment>();

        if (message.Document != null)
        {
            var file = await client.GetFile(message.Document.FileId, ct);
            if (file.FilePath != null)
            {
                using var ms = new MemoryStream();
                await client.DownloadFile(file.FilePath, ms, ct);
                attachments.Add(new Attachment(
                    message.Document.MimeType ?? "application/octet-stream",
                    null,
                    message.Document.FileName,
                    ms.ToArray()
                ));
            }
        }

        var incoming = new IncomingMessage(
            ChannelType: "telegram",
            ChannelId: message.Chat.Id.ToString(),
            UserId: message.From?.Id.ToString() ?? "unknown",
            UserName: message.From?.Username ?? message.From?.FirstName ?? "Unknown",
            Content: message.Text,
            Timestamp: message.Date,
            ReplyToMessageId: message.ReplyToMessage?.MessageId.ToString(),
            Attachments: attachments.Count > 0 ? attachments : null
        );

        if (OnMessageReceived != null)
        {
            await OnMessageReceived(incoming);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken ct)
    {
        Status = ChannelStatus.Error;
        var error = new ChannelError(
            Name,
            exception.Message,
            exception,
            IsRecoverable: true
        );

        OnError?.Invoke(error);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
