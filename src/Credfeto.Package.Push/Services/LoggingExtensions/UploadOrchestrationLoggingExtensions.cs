using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Services.LoggingExtensions;

internal static partial class UploadOrchestrationLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Pushing packages to: {uri}")]
    public static partial void PushingPackagesToServer(this ILogger<UploadOrchestration> logger, Uri uri);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Pushing symbol packages to: {uri}")]
    public static partial void PushingSymbolPackagesToServer(this ILogger<UploadOrchestration> logger, Uri uri);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "No symbols to upload")]
    public static partial void NoSymbolsToUpload(this ILogger<UploadOrchestration> logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Separate Symbol Repo; uses package api to upload")]
    public static partial void SeparateSymbolRepoUsingPackageApiToUpload(this ILogger<UploadOrchestration> logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Separate Symbol Repo; no suitable upload method - upload all as packages to primary")]
    public static partial void SeparateSymbolRepoUploadingAllToPrimary(this ILogger<UploadOrchestration> logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Same Symbol Repo; old format symbols only - uses symbol api to upload at same time")]
    public static partial void SameSymbolRepoOldFormatSymbolsUsingSymbolApiAtSameTime(this ILogger<UploadOrchestration> logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Same Symbol Repo; new format (snupkg) symbols - upload all as packages to primary")]
    public static partial void SameSymbolRepoNewFormatSymbolsAllToPrimary(this ILogger<UploadOrchestration> logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Same Symbol Repo; mixture - old format using symbol api, new format to primary")]
    public static partial void SameSymbolRepoMexedFormatSymbolsOldSymolApiNewToPrimary(this ILogger<UploadOrchestration> logger);
}