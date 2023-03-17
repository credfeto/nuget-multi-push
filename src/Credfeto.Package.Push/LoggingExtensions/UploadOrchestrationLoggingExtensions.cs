using System;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.LoggingExtensions;

internal static partial class UploadOrchestrationLoggingExtensions
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Pushing packages to: {uri}")]
    public static partial void PushingPackagesToServer(this ILogger<UploadOrchestration> logger, Uri uri);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Pushing symbol packages to: {uri}")]
    public static partial void PushingSymbolPackagesToServer(this ILogger<UploadOrchestration> logger, Uri uri);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "No symbols to upload")]
    public static partial void NoSymbolsToUpload(this ILogger<UploadOrchestration> logger);
}