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

                string apiKey = configuration.GetValue<string>(key: @"api-key");

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("ERROR: api-key not specified");

                    return ERROR;
                }

                string source = configuration.GetValue<string>(key: @"source");

                PackageSource packageSource = new PackageSource(name: "Custom", source: source, isEnabled: true, isPersistable: true, isOfficial: true);

                SourceRepository sourceRepository = new SourceRepository(source: packageSource, new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3()));

                PackageUpdateResource packageUpdateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>();
                SymbolPackageUpdateResourceV3 symbolPackageUpdateResource = await sourceRepository.GetResourceAsync<SymbolPackageUpdateResourceV3>();

                IReadOnlyList<string> symbolPackages = packages.Where(p => IsSymbolPackage(p))
                                                               .ToArray();
                IReadOnlyList<string> nonSymbolPackages = packages.Where(p => !IsSymbolPackage(p))
                                                                  .ToArray();

                (string package, bool success)[] results = await Task.WhenAll(nonSymbolPackages.Select(package => PushOnePackageAsync(package: package,
                                                                                                           packages: symbolPackages,
                                                                                                           packageUpdateResource: packageUpdateResource,
                                                                                                           apiKey: apiKey,
                                                                                                           symbolPackageUpdateResource: symbolPackageUpdateResource)));

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
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: {exception.Message}");

                return ERROR;
            }
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
                                                                                      IReadOnlyList<string> packages,
                                                                                      PackageUpdateResource packageUpdateResource,
                                                                                      string apiKey,
                                                                                      SymbolPackageUpdateResourceV3 symbolPackageUpdateResource)
        {
            try
            {
                string expectedSymbol = package.Insert(package.Length - (PACKAGE_EXTENSION.Length + 1), value: ".symbols");
                Console.WriteLine($"Looking for Symbols Package: {expectedSymbol}");

                string? symbolSource = packages.FirstOrDefault(x => StringComparer.InvariantCultureIgnoreCase.Equals(x: x, y: expectedSymbol));

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
    }
}