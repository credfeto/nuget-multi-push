using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Extensions.LoggingExtennsions;

internal static partial class PackageSearchLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to upload {package}: {message}")]
    public static partial void FailedToUploadPackage(this ILogger logger, string package, string message, Exception exception);
}