using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.Push.Configuration;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Exceptions;
using Credfeto.Package.Push.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Package.Push;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"{typeof(Program).Namespace} {ExecutableVersionInformation.ProgramVersion()}");

        try
        {
            IConfigurationRoot configuration = CommandLine.Options(args);

            return await UploadPackagesAsync(configuration);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            return ExitCodes.Error;
        }
    }

    private static async Task<int> UploadPackagesAsync(IConfigurationRoot configuration)
    {
        (string source, string symbolSource, string folder, string apiKey) = LoadConfiguration(configuration);

        IReadOnlyList<string> packages = Searcher.FindMatchingPackages(folder);

        if (packages.Count == 0)
        {
            throw new UploadConfigurationErrorsException("folder does not contain any packages");
        }

        IServiceProvider serviceProvider = ServiceConfiguration.Configure(warningsAsErrors: false);

        IDiagnosticLogger diagnosticLogger = serviceProvider.GetRequiredService<IDiagnosticLogger>();
        IUploadOrchestration uploadOrchestration = serviceProvider.GetRequiredService<IUploadOrchestration>();

        IReadOnlyList<(string package, bool success)> results =
            await uploadOrchestration.PushAllAsync(source: source, symbolSource: symbolSource, packages: packages, apiKey: apiKey, cancellationToken: CancellationToken.None);

        return ProduceSummary(results: results, diagnosticLogger: diagnosticLogger);
    }

    private static int ProduceSummary(IReadOnlyList<(string package, bool success)> results, IDiagnosticLogger diagnosticLogger)
    {
        OutputPackagesAsAssets(results);

        bool success = OutputUploadSummary(results);

        return !success || diagnosticLogger.IsErrored
            ? ExitCodes.Error
            : ExitCodes.Success;
    }

    private static (string source, string symbolSource, string folder, string apiKey) LoadConfiguration(IConfigurationRoot configuration)
    {
        string source = configuration.GetValue<string>(key: @"source")!;
        string symbolSource = configuration.GetValue<string>(key: @"symbol-source")!;

        string folder = configuration.GetValue<string>(key: @"Folder")!;

        if (string.IsNullOrEmpty(source))
        {
            throw new UploadConfigurationErrorsException("Source not specified");
        }

        string apiKey = configuration.GetValue<string>(key: @"api-key")!;

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new UploadConfigurationErrorsException("api-key not specified");
        }

        if (string.IsNullOrEmpty(folder))
        {
            throw new UploadConfigurationErrorsException("folder not specified");
        }

        return (source, symbolSource, folder, apiKey);
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

    private static string GetUploadStatus(bool success)
    {
        return success
            ? "Uploaded"
            : "FAILED";
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