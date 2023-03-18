using System;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.LoggingExtensions;

internal static partial class PackageUploaderLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to upload {package}: {type} {message}")]
    public static partial void FailedToUploadPackage(this ILogger<PackageUploader> logger, string package, string type, string message, Exception exception);
}