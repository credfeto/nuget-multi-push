using System;
using System.Threading.Tasks;
using NuGet.Common;

namespace Credfeto.Package.Push;

internal sealed class ConsoleLogger : ILogger
{
    public void LogDebug(string data)
    {
        Console.WriteLine($"DEBUG: {data}");
    }

    public void LogVerbose(string data)
    {
        Console.WriteLine($"VERBOSE: {data}");
    }

    public void LogInformation(string data)
    {
        Console.WriteLine($"INFO: {data}");
    }

    public void LogMinimal(string data)
    {
        Console.WriteLine($"MINIMAL: {data}");
    }

    public void LogWarning(string data)
    {
        Console.WriteLine($"WARNING: {data}");
    }

    public void LogError(string data)
    {
        Console.WriteLine($"ERROR: {data}");
    }

    public void LogInformationSummary(string data)
    {
        Console.WriteLine($"INFOSUMMARY: {data}");
    }

    public void Log(LogLevel level, string data)
    {
        Console.WriteLine($"{level.GetName().ToUpperInvariant()}: {data}");
    }

    public Task LogAsync(LogLevel level, string data)
    {
        Console.WriteLine($"{level.GetName().ToUpperInvariant()}: {data}");

        return Task.CompletedTask;
    }

    public void Log(ILogMessage message)
    {
        Console.WriteLine($"{message.Level.GetName().ToUpperInvariant()}: {message.Message}");
    }

    public Task LogAsync(ILogMessage message)
    {
        Console.WriteLine($"{message.Level.GetName().ToUpperInvariant()}: {message.Message}");

        return Task.CompletedTask;
    }
}