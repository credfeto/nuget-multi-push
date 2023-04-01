using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Services;

public sealed class LoggerProxy<TLogClass> : ILogger<TLogClass>
{
    private readonly ILogger _diagnosticLogger;

    public LoggerProxy(
        [SuppressMessage(category: "FunFair.CodeAnalysis", checkId: "FFS0024: Logger parameters should be ILogger<T>", Justification = "Not created through DI")] ILogger logger)
    {
        this._diagnosticLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        this._diagnosticLogger.Log(logLevel: logLevel, eventId: eventId, state: state, exception: exception, formatter: formatter);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return this._diagnosticLogger.IsEnabled(logLevel);
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return this._diagnosticLogger.BeginScope(state);
    }
}