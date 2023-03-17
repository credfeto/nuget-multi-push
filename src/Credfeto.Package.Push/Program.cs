using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.Push.Configuration;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Exceptions;
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
            IConfigurationRoot configuration = Config.LoadConfiguration(args);

            (string source, string symbolSource, string folder, string apiKey) = LoadConfiguration(configuration);

            IReadOnlyList<string> packages = Searcher.FindMatchingPackages(folder);

            if (packages.Count == 0)
            {
                Console.WriteLine("ERROR: folder does not contain any packages");

                return ExitCodes.Error;
            }

            IServiceProvider serviceProvider = ServiceConfiguration.Configure();

            IDiagnosticLogger diagnosticLogger = serviceProvider.GetRequiredService<IDiagnosticLogger>();
            IUploadOrchestration uploadOrchestration = serviceProvider.GetRequiredService<IUploadOrchestration>();

            bool result = await uploadOrchestration.PushAllAsync(source: source, symbolSource: symbolSource, packages: packages, apiKey: apiKey, cancellationToken: CancellationToken.None);

            return CheckError(result: result, diagnosticLogger: diagnosticLogger);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            return ExitCodes.Error;
        }
    }

    private static (string source, string symbolSource, string folder, string apiKey) LoadConfiguration(IConfigurationRoot configuration)
    {
        string source = configuration.GetValue<string>(key: @"source")!;
        string symbolSource = configuration.GetValue<string>(key: @"symbol-source")!;

        string folder = configuration.GetValue<string>(key: @"Folder")!;

        if (string.IsNullOrEmpty(source))
        {
            throw new ConfigurationErrorsException("Source not specified");
        }

        string apiKey = configuration.GetValue<string>(key: @"api-key")!;

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationErrorsException("api-key not specified");
        }

        if (string.IsNullOrEmpty(folder))
        {
            throw new ConfigurationErrorsException("folder not specified");
        }

        return (source, symbolSource, folder, apiKey);
    }

    private static int CheckError(bool result, IDiagnosticLogger diagnosticLogger)
    {
        return !result || diagnosticLogger.IsErrored
            ? ExitCodes.Error
            : ExitCodes.Success;
    }
}