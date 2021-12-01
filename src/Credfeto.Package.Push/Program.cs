using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Credfeto.Package.Push
{
    internal static class Program
    {
        private const int SUCCESS = 0;
        private const int ERROR = 1;

        private const string PACKAGE_EXTENSION = ".nupkg";
        private const string SOURCE_PACKAGE_EXTENSION = ".snupkg";
        private const string SYMBOLS_OLD_PACKAGE_EXTENSION = ".symbols" + PACKAGE_EXTENSION;
        private const string SYMBOLS_NEW_PACKAGE_EXTENSION = ".snupkg";
        private const string SEARCH_PATTERN = "*" + PACKAGE_EXTENSION;
        private const string SOURCE_SEARCH_PATTERN = "*" + SOURCE_PACKAGE_EXTENSION;

        private static readonly ILogger NugetLogger = new ConsoleLogger();

        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");

            try
            {
                IConfigurationRoot configuration = LoadConfiguration(args);

                string source = configuration.GetValue<string>(key: @"source");
                string symbolSource = configuration.GetValue<string>(key: @"symbol-source");

                string folder = configuration.GetValue<string>(key: @"Folder");

                if (string.IsNullOrEmpty(folder))
                {
                    Console.WriteLine("ERROR: folder not specified");

                    return ERROR;
                }

                folder = PathHelpers.ConvertToNative(folder);

                IReadOnlyList<string> packages = Directory.GetFiles(path: folder, searchPattern: SEARCH_PATTERN)
                                                          .Concat(Directory.GetFiles(path: folder, searchPattern: SOURCE_SEARCH_PATTERN))
                                                          .ToArray();

                if (packages.Count == 0)
                {
                    Console.WriteLine("ERROR: folder does not contain any packages");

                    return ERROR;
                }

                if (string.IsNullOrEmpty(source))
                {
                    Console.WriteLine("ERROR: source not specified");

                    return ERROR;
                }

                string apiKey = configuration.GetValue<string>(key: @"api-key");

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("ERROR: api-key not specified");

                    return ERROR;
                }

                return await PushAllAsync(source: source, symbolSource: symbolSource, packages: packages, apiKey: apiKey);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: {exception.Message}");

                return ERROR;
            }
        }

        private static async Task<int> PushAllAsync(string source, string symbolSource, IReadOnlyList<string> packages, string apiKey)
        {
            SourceRepository sourceRepository = ConfigureSourceRepository(source);

            PackageUpdateResource packageUpdateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>();
            Console.WriteLine($"Pushing Packages to: {packageUpdateResource.SourceUri}");

            SymbolPackageUpdateResourceV3? symbolPackageUpdateResource = await GetSymbolPackageUpdateSourceAsync(sourceRepository);

            PackageUpdateResource? symbolPackageUpdateResourceAsPackage = null;

            SourceRepository? symbolSourceRepository = null;

            if (!string.IsNullOrWhiteSpace(symbolSource) && symbolPackageUpdateResource == null)
            {
                symbolSourceRepository = ConfigureSourceRepository(symbolSource);

                PackageUpdateResource? resource = await symbolSourceRepository.GetResourceAsync<PackageUpdateResource>();

                if (resource?.SourceUri != null)
                {
                    Console.WriteLine($"Pushing Symbol Packages to: {resource.SourceUri}");
                    symbolPackageUpdateResourceAsPackage = resource;
                }
            }

            IReadOnlyList<string> symbolPackages = ExtractSymbolPackages(packages);
            IReadOnlyList<string> nonSymbolPackages = ExtractProductionPackages(packages);

            IEnumerable<Task<(string package, bool success)>> tasks = BuildUploadTasks(symbolSourceRepository: symbolSourceRepository,
                                                                                       symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                                                       symbolPackageUpdateResourceAsPackage: symbolPackageUpdateResourceAsPackage,
                                                                                       nonSymbolPackages: nonSymbolPackages,
                                                                                       symbolPackages: symbolPackages,
                                                                                       apiKey: apiKey,
                                                                                       packageUpdateResource: packageUpdateResource);

            (string package, bool success)[] results = await Task.WhenAll(tasks);

            OutputPackagesAsAssets(packages);

            return OutputUploadSummary(results);
        }

        private static void OutputPackagesAsAssets(IReadOnlyList<string> packages)
        {
            string? env = Environment.GetEnvironmentVariable("TEAMCITY_VERSION");

            if (string.IsNullOrWhiteSpace(env))
            {
                return;
            }

            foreach (string package in packages)
            {
                Console.WriteLine($"##teamcity[publishArtifacts '{package}']");
            }
        }

        private static IEnumerable<Task<(string package, bool success)>> BuildUploadTasks(SourceRepository? symbolSourceRepository,
                                                                                          SymbolPackageUpdateResourceV3? symbolPackageUpdateResource,
                                                                                          PackageUpdateResource? symbolPackageUpdateResourceAsPackage,
                                                                                          IReadOnlyList<string> nonSymbolPackages,
                                                                                          IReadOnlyList<string> symbolPackages,
                                                                                          string apiKey,
                                                                                          PackageUpdateResource packageUpdateResource)
        {
            if (symbolPackages.Count == 0)
            {
                Console.WriteLine("No symbols to upload");

                return UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource);
            }

            if (symbolSourceRepository != null)
            {
                if (symbolPackageUpdateResourceAsPackage != null)
                {
                    Console.WriteLine("Separate Symbol Repo; uses package api to upload");

                    return UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                        .Concat(UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: symbolPackageUpdateResourceAsPackage));
                }

                Console.WriteLine("Separate Symbol Repo; no suitable upload method - upload all as packages to primary");

                return UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                    .Concat(UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
            }

            if (symbolPackageUpdateResource != null)
            {
                IReadOnlyList<string> oldSymbols = symbolPackages.Where(IsOldSymbolPackage)
                                                                 .ToArray();
                IReadOnlyList<string> newSymbols = symbolPackages.Where(IsNewSymbolPackage)
                                                                 .ToArray();

                if (oldSymbols.Count != 0 && newSymbols.Count == 0)
                {
                    Console.WriteLine("Same Symbol Repo; old format symbols only - uses symbol api to upload at same time");

                    return UploadPackagesWithMatchingSymbols(symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                             nonSymbolPackages: nonSymbolPackages,
                                                             symbolPackages: oldSymbols,
                                                             apiKey: apiKey,
                                                             packageUpdateResource: packageUpdateResource);
                }

                if (oldSymbols.Count == 0 && newSymbols.Count != 0)
                {
                    Console.WriteLine("Same Symbol Repo; new format (snupkg) symbols - upload all as packages to primary");

                    return UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                        .Concat(UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
                }

                Console.WriteLine("Same Symbol Repo; mixture - old format using symbol api, new format to primary");

                return UploadPackagesWithMatchingSymbols(symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                         nonSymbolPackages: nonSymbolPackages,
                                                         symbolPackages: oldSymbols,
                                                         apiKey: apiKey,
                                                         packageUpdateResource: packageUpdateResource)
                    .Concat(UploadPackagesWithoutSymbolLookup(packages: newSymbols, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
            }

            Console.WriteLine("Same Symbol Repo; no suitable upload method - upload all as packages to primary");

            return UploadPackagesWithoutSymbolLookup(packages: nonSymbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource)
                .Concat(UploadPackagesWithoutSymbolLookup(packages: symbolPackages, apiKey: apiKey, packageUpdateResource: packageUpdateResource));
        }

        private static IEnumerable<Task<(string package, bool success)>> UploadPackagesWithMatchingSymbols(SymbolPackageUpdateResourceV3 symbolPackageUpdateResource,
                                                                                                           IReadOnlyList<string> nonSymbolPackages,
                                                                                                           IReadOnlyList<string> symbolPackages,
                                                                                                           string apiKey,
                                                                                                           PackageUpdateResource packageUpdateResource)
        {
            return nonSymbolPackages.Select(package => PushOnePackageAsync(package: package,
                                                                           symbolPackages: symbolPackages,
                                                                           packageUpdateResource: packageUpdateResource,
                                                                           apiKey: apiKey,
                                                                           symbolPackageUpdateResource: symbolPackageUpdateResource));
        }

        private static IEnumerable<Task<(string package, bool success)>> UploadPackagesWithoutSymbolLookup(IReadOnlyList<string> packages, string apiKey, PackageUpdateResource packageUpdateResource)
        {
            return packages.Select(package => PushOnePackageAsync(package: package,
                                                                  packageUpdateResource: packageUpdateResource,
                                                                  apiKey: apiKey,
                                                                  symbolPackageUpdateResource: null,
                                                                  symbolPackages: Array.Empty<string>()));
        }

        private static async Task<SymbolPackageUpdateResourceV3?> GetSymbolPackageUpdateSourceAsync(SourceRepository sourceRepository)
        {
            SymbolPackageUpdateResourceV3? symbolPackageUpdateResource = await sourceRepository.GetResourceAsync<SymbolPackageUpdateResourceV3>();

            if (symbolPackageUpdateResource?.SourceUri == null)
            {
                return null;
            }

            Console.WriteLine($"Pushing Symbol Packages to: {symbolPackageUpdateResource.SourceUri}");

            return symbolPackageUpdateResource;
        }

        private static string[] ExtractProductionPackages(IReadOnlyList<string> packages)
        {
            return packages.Where(p => !IsSymbolPackage(p))
                           .OrderBy(MetaPackageLast)
                           .ThenBy(x => x.ToLowerInvariant())
                           .ToArray();
        }

        private static string[] ExtractSymbolPackages(IReadOnlyList<string> packages)
        {
            return packages.Where(IsSymbolPackage)
                           .OrderBy(MetaPackageLast)
                           .ThenBy(x => x.ToLowerInvariant())
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

            return new SourceRepository(source: packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));
        }

        private static int OutputUploadSummary((string package, bool success)[] results)
        {
            Console.WriteLine("Upload Summary:");
            bool errors = false;

            foreach ((string package, bool success) in results)
            {
                string packageName = Path.GetFileName(package);

                string status = success ? "Uploaded" : "FAILED";
                Console.WriteLine($"* {packageName} : {status}");
                errors |= !success;
            }

            return errors ? ERROR : SUCCESS;
        }

        private static IConfigurationRoot LoadConfiguration(string[] args)
        {
            return new ConfigurationBuilder().AddCommandLine(args: args,
                                                             new Dictionary<string, string>(StringComparer.Ordinal)
                                                             {
                                                                 { @"-folder", @"folder" }, { @"-source", @"source" }, { @"-symbol-source", @"symbol-source" }, { @"-api-key", @"api-key" }
                                                             })
                                             .Build();
        }

        private static bool IsSymbolPackage(string p)
        {
            return IsNewSymbolPackage(p) || IsOldSymbolPackage(p);
        }

        private static bool IsNewSymbolPackage(string p)
        {
            return p.EndsWith(value: SYMBOLS_NEW_PACKAGE_EXTENSION, comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOldSymbolPackage(string p)
        {
            return p.EndsWith(value: SYMBOLS_OLD_PACKAGE_EXTENSION, comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<(string package, bool success)> PushOnePackageAsync(string package,
                                                                                      IReadOnlyList<string> symbolPackages,
                                                                                      PackageUpdateResource packageUpdateResource,
                                                                                      string apiKey,
                                                                                      SymbolPackageUpdateResourceV3? symbolPackageUpdateResource)
        {
            try
            {
                string? symbolSource = FindMatchingSymbolPackage(package: package, symbolPackages: symbolPackages);

                List<string> packagePaths = new() { package };

                await packageUpdateResource.Push(packagePaths: packagePaths,
                                                 symbolSource: symbolSource,
                                                 timeoutInSecond: 800,
                                                 disableBuffering: false,
                                                 getApiKey: _ => apiKey,
                                                 getSymbolApiKey: _ => apiKey,
                                                 noServiceEndpoint: false,
                                                 skipDuplicate: true,
                                                 symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                 log: NugetLogger);

                return (package, success: true);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: Failed to upload {package}: {exception.Message}");

                return (package, success: false);
            }
        }

        private static string? FindMatchingSymbolPackage(string package, IReadOnlyList<string> symbolPackages)
        {
            if (symbolPackages.Count == 0)
            {
                return null;
            }

            int packageExtensionPosition = package.Length - PACKAGE_EXTENSION.Length;

            string baseName = package.Substring(startIndex: 0, length: packageExtensionPosition);

            string expectedSymbolOld = baseName + SYMBOLS_OLD_PACKAGE_EXTENSION;
            string expectedSymbolNew = baseName + SYMBOLS_NEW_PACKAGE_EXTENSION;

            return FindMatchingSymbolByFullName(symbolPackages: symbolPackages, expectedSymbol: expectedSymbolNew) ??
                   FindMatchingSymbolByFullName(symbolPackages: symbolPackages, expectedSymbol: expectedSymbolOld);
        }

        private static string? FindMatchingSymbolByFullName(IReadOnlyList<string> symbolPackages, string expectedSymbol)
        {
            Console.WriteLine($"Looking for Symbols Package: {expectedSymbol}");

            string? symbolSource = symbolPackages.FirstOrDefault(x => StringComparer.InvariantCultureIgnoreCase.Equals(x: x, y: expectedSymbol));

            Console.WriteLine(symbolSource != null ? $"Package package - found symbols {symbolSource}" : $"Package package - no symbols found {expectedSymbol}");

            return symbolSource;
        }
    }
}