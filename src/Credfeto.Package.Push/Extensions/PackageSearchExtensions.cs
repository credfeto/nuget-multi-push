using System;
using System.Collections.Generic;
using System.Linq;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Extensions.LoggingExtennsions;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Extensions;

internal static class PackageSearchExtensions
{
    public static string? FindMatchingSymbolPackage(this IReadOnlyList<string> symbolPackages, string package, ILogger logger)
    {
        if (symbolPackages.Count == 0)
        {
            return null;
        }

        int packageExtensionPosition = package.Length - PackageNaming.PackageExtension.Length;

        string baseName = package.Substring(startIndex: 0, length: packageExtensionPosition);

        string expectedSymbolOld = baseName + PackageNaming.SymbolsOldPackageExtension;
        string expectedSymbolNew = baseName + PackageNaming.SymbolsNewPackageExtension;

        return symbolPackages.FindMatchingSymbolByFullName(expectedSymbol: expectedSymbolNew, logger) ??
               symbolPackages.FindMatchingSymbolByFullName(expectedSymbol: expectedSymbolOld, logger);
    }

    private static string? FindMatchingSymbolByFullName(this IReadOnlyList<string> symbolPackages, string expectedSymbol, ILogger logger)
    {
        logger.LookingForSymbolsPackage($"Looking for Symbols Package: {expectedSymbol}");

        string? symbolSource = symbolPackages.FirstOrDefault(x => StringComparer.InvariantCultureIgnoreCase.Equals(x: x, y: expectedSymbol));

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
        return symbolPackages.Where(PackageNaming.IsNewSymbolPackage)
                             .ToArray();
    }

    public static IReadOnlyList<string> GetOldSymbols(this IReadOnlyList<string> symbolPackages)
    {
        return symbolPackages.Where(PackageNaming.IsOldSymbolPackage)
                             .ToArray();
    }
}