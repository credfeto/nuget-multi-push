using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Extensions;
using Credfeto.Package.Push.Services.LoggingExtensions;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Credfeto.Package.Push.Services;

public sealed class UploadOrchestration : IUploadOrchestration
{
    private readonly ILogger<UploadOrchestration> _logger;
    private readonly IPackageUploader _packageUploader;

    public UploadOrchestration(IPackageUploader packageUploader, ILogger<UploadOrchestration> logger)
    {
        this._packageUploader = packageUploader;
        this._logger = logger;
    }

    public async Task<IReadOnlyList<(string package, bool success)>> PushAllAsync(
        string source,
        string? symbolSource,
        IReadOnlyList<string> packages,
        string apiKey,
        CancellationToken cancellationToken
    )
    {
        SourceRepository sourceRepository = ConfigureSourceRepository(source);

        PackageUpdateResource packageUpdateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>(
            cancellationToken
        );
        this._logger.PushingPackagesToServer(packageUpdateResource.SourceUri);

        SymbolPackageUpdateResourceV3? symbolPackageUpdateResource = await this.GetSymbolPackageUpdateSourceAsync(
            sourceRepository: sourceRepository,
            cancellationToken: cancellationToken
        );

        PackageUpdateResource? symbolPackageUpdateResourceAsPackage = null;

        SourceRepository? symbolSourceRepository = null;

        if (!string.IsNullOrWhiteSpace(symbolSource) && symbolPackageUpdateResource is null)
        {
            symbolSourceRepository = ConfigureSourceRepository(symbolSource);

            PackageUpdateResource? resource = await symbolSourceRepository.GetResourceAsync<PackageUpdateResource>(
                cancellationToken
            );

            if (resource?.SourceUri is not null)
            {
                this._logger.PushingSymbolPackagesToServer(resource.SourceUri);
                symbolPackageUpdateResourceAsPackage = resource;
            }
        }

        IReadOnlyList<string> symbolPackages = ExtractSymbolPackages(packages);
        IReadOnlyList<string> nonSymbolPackages = ExtractProductionPackages(packages);

        IEnumerable<Task<(string package, bool success)>> tasks = this.BuildUploadTasks(
            symbolSourceRepository: symbolSourceRepository,
            symbolPackageUpdateResource: symbolPackageUpdateResource,
            symbolPackageUpdateResourceAsPackage: symbolPackageUpdateResourceAsPackage,
            nonSymbolPackages: nonSymbolPackages,
            symbolPackages: symbolPackages,
            apiKey: apiKey,
            packageUpdateResource: packageUpdateResource
        );

        return await Task.WhenAll(tasks);
    }

    [SuppressMessage("Meziantou.Analyzer", "MA0051: Method is too long", Justification = "Needs Review")]
    private IEnumerable<Task<(string package, bool success)>> BuildUploadTasks(
        SourceRepository? symbolSourceRepository,
        SymbolPackageUpdateResourceV3? symbolPackageUpdateResource,
        PackageUpdateResource? symbolPackageUpdateResourceAsPackage,
        IReadOnlyList<string> nonSymbolPackages,
        IReadOnlyList<string> symbolPackages,
        string apiKey,
        PackageUpdateResource packageUpdateResource
    )
    {
        if (symbolPackages.Count == 0)
        {
            this._logger.NoSymbolsToUpload();

            return this.UploadPackagesWithoutSymbolLookup(
                packages: nonSymbolPackages,
                apiKey: apiKey,
                packageUpdateResource: packageUpdateResource
            );
        }

        if (symbolSourceRepository is not null)
        {
            if (symbolPackageUpdateResourceAsPackage is not null)
            {
                this._logger.SeparateSymbolRepoUsingPackageApiToUpload();

                return this.UploadPackagesWithoutSymbolLookup(
                        packages: nonSymbolPackages,
                        apiKey: apiKey,
                        packageUpdateResource: packageUpdateResource
                    )
                    .Concat(
                        this.UploadPackagesWithoutSymbolLookup(
                            packages: symbolPackages,
                            apiKey: apiKey,
                            packageUpdateResource: symbolPackageUpdateResourceAsPackage
                        )
                    );
            }

            this._logger.SeparateSymbolRepoUploadingAllToPrimary();

            return this.UploadPackagesWithoutSymbolLookup(
                    packages: nonSymbolPackages,
                    apiKey: apiKey,
                    packageUpdateResource: packageUpdateResource
                )
                .Concat(
                    this.UploadPackagesWithoutSymbolLookup(
                        packages: symbolPackages,
                        apiKey: apiKey,
                        packageUpdateResource: packageUpdateResource
                    )
                );
        }

        if (symbolPackageUpdateResource is not null)
        {
            return this.PushWithSeparateUpdateSource(
                symbolPackageUpdateResource: symbolPackageUpdateResource,
                nonSymbolPackages: nonSymbolPackages,
                symbolPackages: symbolPackages,
                apiKey: apiKey,
                packageUpdateResource: packageUpdateResource
            );
        }

        this._logger.SeparateSymbolRepoUploadingAllToPrimary();

        return this.UploadPackagesWithoutSymbolLookup(
                packages: nonSymbolPackages,
                apiKey: apiKey,
                packageUpdateResource: packageUpdateResource
            )
            .Concat(
                this.UploadPackagesWithoutSymbolLookup(
                    packages: symbolPackages,
                    apiKey: apiKey,
                    packageUpdateResource: packageUpdateResource
                )
            );
    }

    private IEnumerable<Task<(string package, bool success)>> PushWithSeparateUpdateSource(
        SymbolPackageUpdateResourceV3 symbolPackageUpdateResource,
        IReadOnlyList<string> nonSymbolPackages,
        IReadOnlyList<string> symbolPackages,
        string apiKey,
        PackageUpdateResource packageUpdateResource
    )
    {
        IReadOnlyList<string> oldSymbols = symbolPackages.GetOldSymbols();
        IReadOnlyList<string> newSymbols = symbolPackages.GetNewSymbols();

        if (oldSymbols.Count != 0 && newSymbols.Count == 0)
        {
            this._logger.SameSymbolRepoOldFormatSymbolsUsingSymbolApiAtSameTime();

            return this.UploadPackagesWithMatchingSymbols(
                symbolPackageUpdateResource: symbolPackageUpdateResource,
                nonSymbolPackages: nonSymbolPackages,
                symbolPackages: oldSymbols,
                apiKey: apiKey,
                packageUpdateResource: packageUpdateResource
            );
        }

        if (oldSymbols.Count == 0 && newSymbols.Count != 0)
        {
            this._logger.SameSymbolRepoNewFormatSymbolsAllToPrimary();

            return this.UploadPackagesWithoutSymbolLookup(
                    packages: nonSymbolPackages,
                    apiKey: apiKey,
                    packageUpdateResource: packageUpdateResource
                )
                .Concat(
                    this.UploadPackagesWithoutSymbolLookup(
                        packages: symbolPackages,
                        apiKey: apiKey,
                        packageUpdateResource: packageUpdateResource
                    )
                );
        }

        this._logger.SameSymbolRepoMexedFormatSymbolsOldSymolApiNewToPrimary();

        return this.UploadPackagesWithMatchingSymbols(
                symbolPackageUpdateResource: symbolPackageUpdateResource,
                nonSymbolPackages: nonSymbolPackages,
                symbolPackages: oldSymbols,
                apiKey: apiKey,
                packageUpdateResource: packageUpdateResource
            )
            .Concat(
                this.UploadPackagesWithoutSymbolLookup(
                    packages: newSymbols,
                    apiKey: apiKey,
                    packageUpdateResource: packageUpdateResource
                )
            );
    }

    private IEnumerable<Task<(string package, bool success)>> UploadPackagesWithMatchingSymbols(
        SymbolPackageUpdateResourceV3 symbolPackageUpdateResource,
        IReadOnlyList<string> nonSymbolPackages,
        IReadOnlyList<string> symbolPackages,
        string apiKey,
        PackageUpdateResource packageUpdateResource
    )
    {
        return nonSymbolPackages.Select(package =>
            this._packageUploader.PushOnePackageAsync(
                package: package,
                symbolPackages: symbolPackages,
                packageUpdateResource: packageUpdateResource,
                apiKey: apiKey,
                symbolPackageUpdateResource: symbolPackageUpdateResource
            )
        );
    }

    private IEnumerable<Task<(string package, bool success)>> UploadPackagesWithoutSymbolLookup(
        IReadOnlyList<string> packages,
        string apiKey,
        PackageUpdateResource packageUpdateResource
    )
    {
        return packages.Select(package =>
            this._packageUploader.PushOnePackageAsync(
                package: package,
                [],
                packageUpdateResource: packageUpdateResource,
                apiKey: apiKey,
                symbolPackageUpdateResource: null
            )
        );
    }

    private async Task<SymbolPackageUpdateResourceV3?> GetSymbolPackageUpdateSourceAsync(
        SourceRepository sourceRepository,
        CancellationToken cancellationToken
    )
    {
        SymbolPackageUpdateResourceV3? symbolPackageUpdateResource =
            await sourceRepository.GetResourceAsync<SymbolPackageUpdateResourceV3>(cancellationToken);

        if (symbolPackageUpdateResource?.SourceUri is null)
        {
            return null;
        }

        this._logger.PushingSymbolPackagesToServer(symbolPackageUpdateResource.SourceUri);

        return symbolPackageUpdateResource;
    }

    private static IReadOnlyList<string> ExtractProductionPackages(IReadOnlyList<string> packages)
    {
        return
        [
            .. packages
                .Where(PackageNaming.IsNotSymbolPackage)
                .OrderBy(MetaPackageLast)
                .ThenBy(keySelector: x => x, comparer: StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static IReadOnlyList<string> ExtractSymbolPackages(IReadOnlyList<string> packages)
    {
        return
        [
            .. packages
                .Where(PackageNaming.IsSymbolPackage)
                .OrderBy(MetaPackageLast)
                .ThenBy(keySelector: x => x, comparer: StringComparer.OrdinalIgnoreCase),
        ];
    }

#if NET9_0_OR_GREATER
    private static bool MetaPackageLast(string packageId)
    {
        ReadOnlySpan<char> span = packageId.AsSpan();

        Range? previousPart = null;
        foreach (Range part in span.Split("."))
        {
            if (IsInteger(span[part]))
            {
                if (previousPart is null)
                {
                    break;
                }

                return IsMetaPackageAllTag(span[previousPart.Value]);
            }

            previousPart = part;
        }

        return false;
    }
#else
    private static bool MetaPackageLast(string packageId)
    {
        string? previousPart = null;
        foreach (string part in packageId.Split("."))
        {
            if (IsInteger(part))
            {
                if (previousPart is null)
                {
                    break;
                }

                return IsMetaPackageAllTag(previousPart);
            }

            previousPart = part;
        }

        return false;
    }
#endif

    private static bool IsInteger(in ReadOnlySpan<char> part)
    {
        return int.TryParse(part, style: NumberStyles.Integer, provider: CultureInfo.InvariantCulture, out _);
    }

    private static bool IsMetaPackageAllTag(in ReadOnlySpan<char> part)
    {
        return Check(part, "all")
            || Check(part, "alL")
            || Check(part, "aLl")
            || Check(part, "aLL")
            || Check(part, "All")
            || Check(part, "AlL")
            || Check(part, "ALl")
            || Check(part, "ALL");

        static bool Check(in ReadOnlySpan<char> lhs, in ReadOnlySpan<char> rhs)
        {
            return lhs.SequenceEqual(rhs);
        }
    }

    private static SourceRepository ConfigureSourceRepository(string source)
    {
        PackageSource packageSource = new(
            source: source,
            name: "Custom",
            isEnabled: true,
            isOfficial: true,
            isPersistable: true
        );

        return new(source: packageSource, [.. Repository.Provider.GetCoreV3()]);
    }
}
