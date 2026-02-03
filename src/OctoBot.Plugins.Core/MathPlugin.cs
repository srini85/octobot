using System.ComponentModel;
using Microsoft.Extensions.AI;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Plugins.Core;

public class MathPlugin : IPlugin
{
    private readonly MathFunctions _functions = new();

    public PluginMetadata Metadata => new(
        Id: "math",
        Name: "Math",
        Description: "Provides mathematical calculation functions",
        Version: "1.0.0",
        Author: "OctoBot"
    );

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(_functions.Add, name: "Math_Add");
        yield return AIFunctionFactory.Create(_functions.Subtract, name: "Math_Subtract");
        yield return AIFunctionFactory.Create(_functions.Multiply, name: "Math_Multiply");
        yield return AIFunctionFactory.Create(_functions.Divide, name: "Math_Divide");
        yield return AIFunctionFactory.Create(_functions.SquareRoot, name: "Math_SquareRoot");
        yield return AIFunctionFactory.Create(_functions.Power, name: "Math_Power");
        yield return AIFunctionFactory.Create(_functions.Percentage, name: "Math_Percentage");
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

public class MathFunctions
{
    [Description("Adds two numbers together")]
    public double Add(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a + b;
    }

    [Description("Subtracts the second number from the first")]
    public double Subtract(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a - b;
    }

    [Description("Multiplies two numbers")]
    public double Multiply(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a * b;
    }

    [Description("Divides the first number by the second")]
    public string Divide(
        [Description("The dividend")] double a,
        [Description("The divisor")] double b)
    {
        if (b == 0) return "Cannot divide by zero";
        return (a / b).ToString();
    }

    [Description("Calculates the square root of a number")]
    public string SquareRoot([Description("The number")] double n)
    {
        if (n < 0) return "Cannot calculate square root of negative number";
        return Math.Sqrt(n).ToString();
    }

    [Description("Raises a number to a power")]
    public double Power(
        [Description("The base number")] double baseNum,
        [Description("The exponent")] double exponent)
    {
        return Math.Pow(baseNum, exponent);
    }

    [Description("Calculates the percentage of a number")]
    public double Percentage(
        [Description("The value")] double value,
        [Description("The percentage")] double percent)
    {
        return value * percent / 100;
    }
}
