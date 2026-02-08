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
}
