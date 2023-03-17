using System;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.LoggingExtensions;

internal static partial class PackageUploaderLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to upload {package}: {message}")]
    public static partial void FailedToUploadPackage(this ILogger<PackageUploader> logger, string package, string message, Exception exception);
}