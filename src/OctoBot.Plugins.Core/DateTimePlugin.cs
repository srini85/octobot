using System.ComponentModel;
using Microsoft.Extensions.AI;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Plugins.Core;

public class DateTimePlugin : IPlugin
{
    private readonly DateTimeFunctions _functions = new();

    public PluginMetadata Metadata => new(
        Id: "datetime",
        Name: "Date & Time",
        Description: "Provides date and time related functions",
        Version: "1.0.0",
        Author: "OctoBot"
    );

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(_functions.GetCurrentDateTimeUtc, name: "DateTime_GetCurrentDateTimeUtc");
        yield return AIFunctionFactory.Create(_functions.GetCurrentDateTime, name: "DateTime_GetCurrentDateTime");
        yield return AIFunctionFactory.Create(_functions.GetDayOfWeek, name: "DateTime_GetDayOfWeek");
        yield return AIFunctionFactory.Create(_functions.CalculateDateDifference, name: "DateTime_CalculateDateDifference");
    }

    public Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

public class DateTimeFunctions
{
    [Description("Gets the current date and time in UTC")]
    public string GetCurrentDateTimeUtc()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
    }

    [Description("Gets the current date and time in a specific timezone")]
    public string GetCurrentDateTime([Description("The timezone ID (e.g., 'America/New_York', 'Europe/London')")] string timezoneId)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return localTime.ToString("yyyy-MM-dd HH:mm:ss") + $" ({timezoneId})";
        }
        catch (TimeZoneNotFoundException)
        {
            return $"Unknown timezone: {timezoneId}";
        }
    }

    [Description("Gets the current day of the week")]
    public string GetDayOfWeek()
    {
        return DateTime.UtcNow.DayOfWeek.ToString();
    }

    [Description("Calculates the difference between two dates")]
    public string CalculateDateDifference(
        [Description("Start date in format yyyy-MM-dd")] string startDate,
        [Description("End date in format yyyy-MM-dd")] string endDate)
    {
        if (!DateTime.TryParse(startDate, out var start) || !DateTime.TryParse(endDate, out var end))
        {
            return "Invalid date format. Please use yyyy-MM-dd";
        }

        var diff = end - start;
        return $"{Math.Abs(diff.Days)} days";
    }
}
