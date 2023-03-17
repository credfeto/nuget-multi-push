using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Extensions;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Credfeto.Package.Push.Services;

public sealed class Pusher : IPusher
{
    private readonly ILogger<Pusher> _logger;
    private readonly IUploader _uploader;

    public Pusher(IUploader uploader, ILogger<Pusher> logger)
    {
        this._uploader = uploader;
        this._logger = logger;
    }

    public async Task<bool> PushAllAsync(string source, string symbolSource, IReadOnlyList<string> packages, string apiKey, CancellationToken cancellationToken)
    {
        SourceRepository sourceRepository = ConfigureSourceRepository(source);

        PackageUpdateResource packageUpdateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>(cancellationToken);
        this._logger.LogInformation($"Pushing Packages to: {packageUpdateResource.SourceUri}");

        SymbolPackageUpdateResourceV3? symbolPackageUpdateResource = await this.GetSymbolPackageUpdateSourceAsync(sourceRepository: sourceRepository, cancellationToken: cancellationToken);

        PackageUpdateResource? symbolPackageUpdateResourceAsPackage = null;

        SourceRepository? symbolSourceRepository = null;

        if (!string.IsNullOrWhiteSpace(symbolSource) && symbolPackageUpdateResource == null)
        {
            symbolSourceRepository = ConfigureSourceRepository(symbolSource);

            PackageUpdateResource? resource = await symbolSourceRepository.GetResourceAsync<PackageUpdateResource>(cancellationToken);

            if (resource?.SourceUri != null)
            {
                this._logger.LogInformation($"Pushing Symbol Packages to: {resource.SourceUri}");
                symbolPackageUpdateResourceAsPackage = resource;
            }
        }

        IReadOnlyList<string> symbolPackages = ExtractSymbolPackages(packages);
        IReadOnlyList<string> nonSymbolPackages = ExtractProductionPackages(packages);

        IEnumerable<Task<(string package, bool success)>> tasks = this.BuildUploadTasks(symbolSourceRepository: symbolSourceRepository,
                                                                                        symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                                                        symbolPackageUpdateResourceAsPackage: symbolPackageUpdateResourceAsPackage,
                                                                                        nonSymbolPackages: nonSymbolPackages,
                                                                                        symbolPackages: symbolPackages,
                                                                                        apiKey: apiKey,
                                                                                        packageUpdateResource: packageUpdateResource);

        (string package, bool success)[] results = await Task.WhenAll(tasks);

        this.OutputPackagesAsAssets(packages);

        return this.OutputUploadSummary(results);
    }

    private void OutputPackagesAsAssets(IReadOnlyList<string> packages)
    {
        string? env = Environment.GetEnvironmentVariable("TEAMCITY_VERSION");

        if (string.IsNullOrWhiteSpace(env))
        {
            return;
        }

        foreach (string package in packages)
        {
            this._logger.LogInformation($"##teamcity[publishArtifacts '{package}']");
        }
    }

    private IEnumerable<Task<(string package, bool success)>> BuildUploadTasks(SourceRepository? symbolSourceRepository,
                                                                               SymbolPackageUpdateResourceV3? symbolPackageUpdateResource,
                                                                               PackageUpdateResource? symbolPackageUpdateResourceAsPackage,
                                                                               IReadOnlyList<string> nonSymbolPackages,
                                                                               IReadOnlyList<string> symbolPackages,
                                                                               string apiKey,
                                                                               PackageUpdateResource packageUpdateResource)
    {
        if (symbolPackages.Count == 0)
        {
            this._logger.LogWarning("No symbols to upload");

            return this.UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource);
        }

        if (symbolSourceRepository != null)
        {
            if (symbolPackageUpdateResourceAsPackage != null)
            {
                this._logger.LogInformation("Separate Symbol Repo; uses package api to upload");

                return this.UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                           .Concat(this.UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: symbolPackageUpdateResourceAsPackage));
            }

            this._logger.LogInformation("Separate Symbol Repo; no suitable upload method - upload all as packages to primary");

            return this.UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                       .Concat(this.UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
        }

        if (symbolPackageUpdateResource != null)
        {
            return this.PushWithSeparateUpdateSource(symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                     nonSymbolPackages: nonSymbolPackages,
                                                     symbolPackages: symbolPackages,
                                                     apiKey: apiKey,
                                                     packageUpdateResource: packageUpdateResource);
        }

        this._logger.LogInformation("Same Symbol Repo; no suitable upload method - upload all as packages to primary");

        return this.UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                   .Concat(this.UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
    }

    private IEnumerable<Task<(string package, bool success)>> PushWithSeparateUpdateSource(SymbolPackageUpdateResourceV3 symbolPackageUpdateResource,
                                                                                           IReadOnlyList<string> nonSymbolPackages,
                                                                                           IReadOnlyList<string> symbolPackages,
                                                                                           string apiKey,
                                                                                           PackageUpdateResource packageUpdateResource)
    {
        IReadOnlyList<string> oldSymbols = symbolPackages.GetOldSymbols();
        IReadOnlyList<string> newSymbols = symbolPackages.GetNewSymbols();

        if (oldSymbols.Count != 0 && newSymbols.Count == 0)
        {
            this._logger.LogInformation("Same Symbol Repo; old format symbols only - uses symbol api to upload at same time");

            return this.UploadPackagesWithMatchingSymbols(symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                          nonSymbolPackages: nonSymbolPackages,
                                                          symbolPackages: oldSymbols,
                                                          apiKey: apiKey,
                                                          packageUpdateResource: packageUpdateResource);
        }

        if (oldSymbols.Count == 0 && newSymbols.Count != 0)
        {
            this._logger.LogInformation("Same Symbol Repo; new format (snupkg) symbols - upload all as packages to primary");

            return this.UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                       .Concat(this.UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
        }

        this._logger.LogInformation("Same Symbol Repo; mixture - old format using symbol api, new format to primary");

        return this.UploadPackagesWithMatchingSymbols(symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                      nonSymbolPackages: nonSymbolPackages,
                                                      symbolPackages: oldSymbols,
                                                      apiKey: apiKey,
                                                      packageUpdateResource: packageUpdateResource)
                   .Concat(this.UploadPackagesWithoutSymbolLookup(packages: newSymbols, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
    }

    private IEnumerable<Task<(string package, bool success)>> UploadPackagesWithMatchingSymbols(SymbolPackageUpdateResourceV3 symbolPackageUpdateResource,
                                                                                                IReadOnlyList<string> nonSymbolPackages,
                                                                                                IReadOnlyList<string> symbolPackages,
                                                                                                string apiKey,
                                                                                                PackageUpdateResource packageUpdateResource)
    {
        return nonSymbolPackages.Select(package => this._uploader.PushOnePackageAsync(package: package,
                                                                                      symbolPackages: symbolPackages,
                                                                                      packageUpdateResource: packageUpdateResource,
                                                                                      apiKey: apiKey,
                                                                                      symbolPackageUpdateResource: symbolPackageUpdateResource));
    }

    private IEnumerable<Task<(string package, bool success)>> UploadPackagesWithoutSymbolLookup(IReadOnlyList<string> packages, string apiKey, PackageUpdateResource packageUpdateResource)
    {
        return packages.Select(package => this._uploader.PushOnePackageAsync(package: package,
                                                                             packageUpdateResource: packageUpdateResource,
                                                                             apiKey: apiKey,
                                                                             symbolPackageUpdateResource: null,
                                                                             symbolPackages: Array.Empty<string>()));
    }

    private async Task<SymbolPackageUpdateResourceV3?> GetSymbolPackageUpdateSourceAsync(SourceRepository sourceRepository, CancellationToken cancellationToken)
    {
        SymbolPackageUpdateResourceV3? symbolPackageUpdateResource = await sourceRepository.GetResourceAsync<SymbolPackageUpdateResourceV3>(cancellationToken);

        if (symbolPackageUpdateResource?.SourceUri == null)
        {
            return null;
        }

        this._logger.LogInformation($"Pushing Symbol Packages to: {symbolPackageUpdateResource.SourceUri}");

        return symbolPackageUpdateResource;
    }

    private static IReadOnlyList<string> ExtractProductionPackages(IReadOnlyList<string> packages)
    {
        return packages.Where(PackageNaming.IsNotSymbolPackage)
                       .OrderBy(MetaPackageLast)
                       .ThenBy(keySelector: x => x, comparer: StringComparer.OrdinalIgnoreCase)
                       .ToArray();
    }

    private static IReadOnlyList<string> ExtractSymbolPackages(IReadOnlyList<string> packages)
    {
        return packages.Where(PackageNaming.IsSymbolPackage)
                       .OrderBy(MetaPackageLast)
                       .ThenBy(keySelector: x => x, comparer: StringComparer.OrdinalIgnoreCase)
                       .ToArray();
    }

    private static bool MetaPackageLast(string packageId)
    {
        string[] parts = packageId.Split(".");

        for (int part = 0; part < parts.Length; ++part)
        {
            if (int.TryParse(parts[part], style: NumberStyles.Integer, provider: CultureInfo.InvariantCulture, out int _))
            {
                int previousPart = part - 1;

                if (previousPart < 0)
                {
                    break;
                }

                return StringComparer.InvariantCultureIgnoreCase.Equals(parts[previousPart], y: "All");
            }
        }

        return false;
    }

    private static SourceRepository ConfigureSourceRepository(string source)
    {
        PackageSource packageSource = new(name: "Custom", source: source, isEnabled: true, isPersistable: true, isOfficial: true);

        return new(source: packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));
    }

    private bool OutputUploadSummary(IReadOnlyList<(string package, bool success)> results)
    {
        this._logger.LogInformation("Upload Summary:");
        bool errors = false;

        foreach ((string package, bool success) in results)
        {
            string packageName = Path.GetFileName(package);

            string status = success
                ? "Uploaded"
                : "FAILED";
            this._logger.LogInformation($"* {packageName} : {status}");
            errors |= !success;
        }

        return !errors;
    }
}