using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Extensions.LoggingExtennsions;

internal static partial class PackageSearchLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Retrying transient exception {typeName}, on attempt {retryCount} of {maxRetries}. Current delay is {delay}: {details}")]
    public static partial void LogAndDispatchTransientException(this ILogger logger, string typeName);
}