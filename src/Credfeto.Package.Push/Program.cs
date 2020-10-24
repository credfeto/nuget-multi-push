using System;
using System.Collections.Generic;
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
        private const string SYMBOLS_PACKAGE_EXTENSION = ".symbols" + PACKAGE_EXTENSION;
        private const string SEARCH_PATTERN = "*" + PACKAGE_EXTENSION;

        private static readonly ILogger NugetLogger = new ConsoleLogger();

        private static async Task<int> Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = LoadConfiguration(args);

                string source = configuration.GetValue<string>(key: @"source");

                string folder = configuration.GetValue<string>(key: @"Folder");

                if (string.IsNullOrEmpty(folder))
                {
                    Console.WriteLine("ERROR: folder not specified");

                    return ERROR;
                }

                IReadOnlyList<string> packages = Directory.GetFiles(path: folder, searchPattern: SEARCH_PATTERN);

                if (!packages.Any())
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

                SourceRepository sourceRepository = ConfigureSourceRepository(source);

                PackageUpdateResource packageUpdateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>();
                Console.WriteLine($"Pushing Packages to: {packageUpdateResource.SourceUri}");

                SymbolPackageUpdateResourceV3 symbolPackageUpdateResource = await sourceRepository.GetResourceAsync<SymbolPackageUpdateResourceV3>();

                Console.WriteLine($"Pushing Symbol Packages to: {symbolPackageUpdateResource.SourceUri}");

                IReadOnlyList<string> symbolPackages = ExtractSymbolPackages(packages);
                IReadOnlyList<string> nonSymbolPackages = ExtractProductionPackages(packages);

                IReadOnlyList<string> uploadOrder = symbolPackageUpdateResource.SourceUri != null
                    ? nonSymbolPackages
                    : nonSymbolPackages.Concat(symbolPackages)
                                       .ToArray();

                IReadOnlyList<string> symbolSearch = symbolPackageUpdateResource.SourceUri != null ? symbolPackages : Array.Empty<string>();

                (string package, bool success)[] results = await Task.WhenAll(uploadOrder.Select(package => PushOnePackageAsync(package: package,
                                                                                                                                symbolPackages: symbolSearch,
                                                                                                                                packageUpdateResource: packageUpdateResource,
                                                                                                                                apiKey: apiKey,
                                                                                                                                symbolPackageUpdateResource: symbolPackageUpdateResource)));

                return OutputUploadSummary(results);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: {exception.Message}");

                return ERROR;
            }
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
                if (int.TryParse(parts[part], out int _))
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
            PackageSource packageSource = new PackageSource(name: "Custom", source: source, isEnabled: true, isPersistable: true, isOfficial: true);

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
            return new ConfigurationBuilder().AddCommandLine(args: args, new Dictionary<string, string> {{@"-folder", @"folder"}, {@"-source", @"source"}, {@"-api-key", @"api-key"}})
                                             .Build();
        }

        private static bool IsSymbolPackage(string p)
        {
            return p.EndsWith(value: SYMBOLS_PACKAGE_EXTENSION, comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<(string package, bool success)> PushOnePackageAsync(string package,
                                                                                      IReadOnlyList<string> symbolPackages,
                                                                                      PackageUpdateResource packageUpdateResource,
                                                                                      string apiKey,
                                                                                      SymbolPackageUpdateResourceV3 symbolPackageUpdateResource)
        {
            try
            {
                string? symbolSource = FindMatchingSymbolPackage(package: package, symbolPackages: symbolPackages);

                await packageUpdateResource.Push(packagePath: package,
                                                 symbolSource: symbolSource,
                                                 timeoutInSecond: 800,
                                                 disableBuffering: false,
                                                 getApiKey: x => apiKey,
                                                 getSymbolApiKey: x => apiKey,
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

            string expectedSymbol = package.Insert(package.Length - PACKAGE_EXTENSION.Length, value: ".symbols");
            Console.WriteLine($"Looking for Symbols Package: {expectedSymbol}");

            string? symbolSource = symbolPackages.FirstOrDefault(x => StringComparer.InvariantCultureIgnoreCase.Equals(x: x, y: expectedSymbol));

            if (symbolSource != null)
            {
                Console.WriteLine($"Package package - found symbols {symbolSource}");
            }
            else
            {
                Console.WriteLine($"Package package - no symbols found {expectedSymbol}");
            }

            return symbolSource;
        }
    }
}