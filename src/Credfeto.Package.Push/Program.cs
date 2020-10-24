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

        private static readonly ILogger NugetLogger = new ConsoleLogger();

        private static async Task<int> Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                                                   .AddCommandLine(args: args, new Dictionary<string, string> {{@"-folder", @"folder"}, {@"-source", @"source"}, {@"-api-key", @"api-key"}})
                                                   .Build();

                string folder = configuration.GetValue<string>(key: @"Folder");

                if (string.IsNullOrEmpty(folder))
                {
                    Console.WriteLine("ERROR: folder not specified");

                    return ERROR;
                }

                IReadOnlyList<string> packages = Directory.GetFiles(path: folder, searchPattern: "*.nupkg");

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

                IReadOnlyList<string> nonSymbolPackages = packages.Where(p => !p.EndsWith(value: ".symbols.nupkg", comparisonType: StringComparison.OrdinalIgnoreCase))
                                                                  .ToArray();

                foreach (var package in nonSymbolPackages)
                {
                    await PushOnePackageAsync(package: package,
                                              packages: packages,
                                              packageUpdateResource: packageUpdateResource,
                                              apiKey: apiKey,
                                              symbolPackageUpdateResource: symbolPackageUpdateResource);
                }

                return SUCCESS;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR: {exception.Message}");

                return ERROR;
            }
        }

        private static async Task<(string package, bool success)> PushOnePackageAsync(string package,
                                                                                      IReadOnlyList<string> packages,
                                                                                      PackageUpdateResource packageUpdateResource,
                                                                                      string apiKey,
                                                                                      SymbolPackageUpdateResourceV3 symbolPackageUpdateResource)
        {
            try
            {
                string expectedSymbol = package.Insert(package.Length - 5, value: ".symbols");

                string? symbolSource = packages.FirstOrDefault(x => StringComparer.InvariantCultureIgnoreCase.Equals(x: x, y: expectedSymbol));

                await packageUpdateResource.Push(packagePath: package,
                                                 symbolSource: symbolSource,
                                                 timeoutInSecond: 100,
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