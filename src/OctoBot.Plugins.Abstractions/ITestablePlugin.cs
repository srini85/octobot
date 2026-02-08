namespace OctoBot.Plugins.Abstractions;

public interface ITestablePlugin
{
    Task<(bool Success, string Message)> TestConnectionAsync();
}
