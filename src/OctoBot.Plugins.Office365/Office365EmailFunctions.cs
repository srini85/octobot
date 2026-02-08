using System.ComponentModel;

namespace OctoBot.Plugins.Office365;

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

    [Description("Get a list of recent emails with their IDs, subjects, and senders. Use this to find an email the user wants to reply to.")]
    public async Task<string> GetRecentEmails(
        [Description("Number of recent emails to retrieve (default: 10)")] int count = 10)
    {
        return await _plugin.GetRecentEmailsAsync(count);
    }

    [Description("Reply to an email. IMPORTANT: Before calling this, you MUST first draft the reply text and show it to the user for their approval. Only call this function after the user explicitly confirms they want to send the reply.")]
    public async Task<string> ReplyToEmail(
        [Description("The ID of the email to reply to (from GetRecentEmails)")] string messageId,
        [Description("The reply message body text")] string replyBody)
    {
        return await _plugin.ReplyToEmailAsync(messageId, replyBody);
    }
}
