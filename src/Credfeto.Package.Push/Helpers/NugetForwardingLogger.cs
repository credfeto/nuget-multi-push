using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using ILogger = NuGet.Common.ILogger;
using LogLevel = NuGet.Common.LogLevel;

namespace Credfeto.Package.Push.Helpers;

public sealed class NugetForwardingLogger : ILogger
{
    private readonly ILogger<NugetForwardingLogger> _logger;

    public NugetForwardingLogger(ILogger<NugetForwardingLogger> logger)
    {
        this._logger = logger;
    }

    public void LogDebug(string data)
    {
        this._logger.LogDebug(data);
    }

    public void LogVerbose(string data)
    {
        this._logger.LogTrace(data);
    }

    public void LogInformation(string data)
    {
        this._logger.LogInformation(data);
    }

    public void LogMinimal(string data)
    {
        this._logger.LogInformation(data);
    }

    public void LogWarning(string data)
    {
        this._logger.LogWarning(data);
    }

    public void LogError(string data)
    {
        this._logger.LogError(data);
    }

    public void LogInformationSummary(string data)
    {
        this._logger.LogInformation(data);
    }

    public void Log(LogLevel level, string data)
    {
        switch (level)
        {
            case LogLevel.Debug:
                this.LogDebug(data);

                break;
            case LogLevel.Verbose:
                this.LogVerbose(data);

                break;
            case LogLevel.Information:
                this.LogInformation(data);

                break;
            case LogLevel.Minimal:
                this.LogMinimal(data);

                break;
            case LogLevel.Warning:
                this.LogWarning(data);

                break;
            case LogLevel.Error:
                this.LogError(data);

                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    actualValue: level,
                    message: "Unknown log level"
                );
        }
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
