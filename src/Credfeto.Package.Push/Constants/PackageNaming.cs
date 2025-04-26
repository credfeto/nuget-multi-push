using System;

namespace Credfeto.Package.Push.Constants;

internal static class PackageNaming
{
    public const string PackageExtension = ".nupkg";
    public const string SymbolsOldPackageExtension = ".symbols" + PackageExtension;
    public const string SymbolsNewPackageExtension = ".snupkg";
    public const string SearchPattern = "*" + PackageExtension;
    public const string SourceSearchPattern = "*" + SymbolsNewPackageExtension;

    public static bool IsSymbolPackage(string p)
    {
        return IsNewSymbolPackage(p) || IsOldSymbolPackage(p);
    }

    public static bool IsNotSymbolPackage(string p)
    {
        return !IsSymbolPackage(p);
    }

    public static bool IsNewSymbolPackage(string p)
    {
        return p.EndsWith(value: SymbolsNewPackageExtension, comparisonType: StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOldSymbolPackage(string p)
    {
        return p.EndsWith(value: SymbolsOldPackageExtension, comparisonType: StringComparison.OrdinalIgnoreCase);
    }
}
