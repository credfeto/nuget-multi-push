using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Services.LoggingExtensions;

internal static partial class UploaderLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Retrying transient exception {typeName}, on attempt {retryCount} of {maxRetries}. Current delay is {delay}: {details}")]
    public static partial void LogAndDispatchTransientException(this ILogger<Uploader> logger, string typeName, int retryCount, int maxRetries, TimeSpan delay, string details, Exception exception);
}