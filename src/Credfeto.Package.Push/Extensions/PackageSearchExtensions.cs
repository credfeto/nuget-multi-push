using System;
using System.Collections.Generic;
using System.Linq;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Extensions.LoggingExtennsions;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Extensions;

internal static class PackageSearchExtensions
{
    public static string? FindMatchingSymbolPackage(
        this IReadOnlyList<string> symbolPackages,
        string package,
        ILogger logger
    )
    {
        if (symbolPackages.Count == 0)
        {
            return null;
        }

        int packageExtensionPosition = package.Length - PackageNaming.PackageExtension.Length;

        string baseName = package[..packageExtensionPosition];

        string expectedSymbolOld = baseName + PackageNaming.SymbolsOldPackageExtension;
        string expectedSymbolNew = baseName + PackageNaming.SymbolsNewPackageExtension;

        return symbolPackages.FindMatchingSymbolByFullName(expectedSymbol: expectedSymbolNew, logger: logger)
            ?? symbolPackages.FindMatchingSymbolByFullName(expectedSymbol: expectedSymbolOld, logger: logger);
    }

    private static string? FindMatchingSymbolByFullName(
        this IReadOnlyList<string> symbolPackages,
        string expectedSymbol,
        ILogger logger
    )
    {
        logger.LookingForSymbolsPackage($"Looking for Symbols Package: {expectedSymbol}");

        string? symbolSource = symbolPackages.FirstOrDefault(x =>
            StringComparer.OrdinalIgnoreCase.Equals(x: x, y: expectedSymbol)
        );

        if (symbolSource is not null)
        {
            logger.SymbolPackageFound(symbolSource);
        }
        else
        {
            logger.SymbolPackageNotFound(expectedSymbol);
        }

        return symbolSource;
    }

    public static IReadOnlyList<string> GetNewSymbols(this IReadOnlyList<string> symbolPackages)
    {
        return symbolPackages.Filter(PackageNaming.IsNewSymbolPackage);
    }

    public static IReadOnlyList<string> GetOldSymbols(this IReadOnlyList<string> symbolPackages)
    {
        return symbolPackages.Filter(PackageNaming.IsOldSymbolPackage);
    }

    private static IReadOnlyList<string> Filter(this IReadOnlyList<string> symbolPackages, Func<string, bool> match)
    {
        return [.. symbolPackages.Where(match)];
    }
}
