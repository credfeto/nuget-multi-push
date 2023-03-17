using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Services;

/// <summary>
///     Proxy for logging.
/// </summary>
/// <typeparam name="TLogClass">The log class.</typeparam>
public sealed class LoggerProxy<TLogClass> : ILogger<TLogClass>
{
    private readonly ILogger _diagnosticLogger;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public LoggerProxy([SuppressMessage(category: "FunFair.CodeAnalysis", checkId: "FFS0024: Logger parameters should be ILogger<T>", Justification = "Not created through DI")] ILogger logger)
    {
        this._diagnosticLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        this._diagnosticLogger.Log(logLevel: logLevel, eventId: eventId, state: state, exception: exception, formatter: formatter);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return this._diagnosticLogger.IsEnabled(logLevel);
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return this._diagnosticLogger.BeginScope(state);
    }
}