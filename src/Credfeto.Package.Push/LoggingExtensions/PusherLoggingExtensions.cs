using System;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.LoggingExtensions;

internal static partial class PusherLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Retrying transient exception {typeName}, on attempt {retryCount} of {maxRetries}. Current delay is {delay}: {details}")]
    public static partial void LogAndDispatchTransientException(this ILogger<Pusher> logger, string typeName, int retryCount, int maxRetries, TimeSpan delay, string details, Exception exception);
}