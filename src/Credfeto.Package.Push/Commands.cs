using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cocona;
using Credfeto.Package.Push.Exceptions;

namespace Credfeto.Package.Push;

[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by Cocona")]
internal sealed class Commands
{
    private readonly IUploadOrchestration _uploadOrchestration;

    public Commands(IUploadOrchestration uploadOrchestration)
    {
        this._uploadOrchestration = uploadOrchestration;
    }

    [Command(Description = "Uploads all packages in the folder")]
    [PrimaryCommand]
    public async Task UploadPackagesAsync([Option(name: "source", ['s'], Description = "NuGet Feed to upload packages to")] string source,
                                          [Option(name: "folder", ['f'], Description = "Folder containing packages to upload")] string folder,
                                          [Option(name: "api-key", ['a'], Description = "Api Key for uploading packages")] string apiKey,
                                          [Option("symbol-source", Description = "NuGet Feed to upload symbol packages to")] string? symbolSource)
    {
        IReadOnlyList<string> packages = Searcher.FindMatchingPackages(folder);

        if (packages.Count == 0)
        {
            throw new UploadConfigurationErrorsException("folder does not contain any packages");
        }

        IReadOnlyList<(string package, bool success)> results =
            await this._uploadOrchestration.PushAllAsync(source: source, symbolSource: symbolSource, packages: packages, apiKey: apiKey, cancellationToken: CancellationToken.None);

        ProduceSummary(results: results);
    }

    private static void ProduceSummary(IReadOnlyList<(string package, bool success)> results)
    {
        OutputPackagesAsAssets(results);

        bool success = OutputUploadSummary(results);

        if (!success)
        {
            throw new UploadFailedException();
        }
    }

    private static bool OutputUploadSummary(IReadOnlyList<(string package, bool success)> results)
    {
        Console.WriteLine("Upload Summary:");
        int succeeded = 0;
        int errors = 0;

        foreach ((string package, bool success) in results)
        {
            string packageName = Path.GetFileName(package);

            if (success)
            {
                Console.WriteLine($"* {packageName} : Uploaded");
                ++succeeded;
            }
            else
            {
                Console.WriteLine($"* {packageName} : FAILED");
                ++errors;
            }
        }

        Console.WriteLine($"Total Packages: {results.Count}");
        Console.WriteLine($"Succeeded: {succeeded}");
        Console.WriteLine($"Failures: {errors}");

        return errors == 0;
    }

    private static void OutputPackagesAsAssets(IReadOnlyList<(string package, bool success)> packages)
    {
        string? env = Environment.GetEnvironmentVariable("TEAMCITY_VERSION");

        if (string.IsNullOrWhiteSpace(env))
        {
            return;
        }

        foreach (string package in packages.Where(x => x.success)
                                           .Select(x => x.package))
        {
            Console.WriteLine($"##teamcity[publishArtifacts '{package}']");
        }
    }
}