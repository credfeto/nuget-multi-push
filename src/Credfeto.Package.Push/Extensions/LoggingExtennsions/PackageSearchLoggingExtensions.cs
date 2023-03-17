using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Extensions.LoggingExtennsions;

internal static partial class PackageSearchLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Looking for Symbols Package: {expectedSymbol}")]
    [Conditional("DEBUG")]
    public static partial void LookingForSymbolsPackage(this ILogger logger, string expectedSymbol);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Package - found symbols {symbolSource}")]
    [Conditional("DEBUG")]
    public static partial void SymbolPackageFound(this ILogger logger, string symbolSource);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Package - no symbols found {expectedSymbol}")]
    [Conditional("DEBUG")]
    public static partial void SymbolPackageNotFound(this ILogger logger, string expectedSymbol);
}