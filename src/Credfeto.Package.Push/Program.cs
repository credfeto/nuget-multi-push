using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Package.Push.Configuration;
using Credfeto.Package.Push.Constants;
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

            string source = configuration.GetValue<string>(key: @"source")!;
            string symbolSource = configuration.GetValue<string>(key: @"symbol-source")!;

            string folder = configuration.GetValue<string>(key: @"Folder")!;

            if (string.IsNullOrEmpty(folder))
            {
                Console.WriteLine("ERROR: folder not specified");

                return ExitCodes.ERROR;
            }

            folder = PathHelpers.ConvertToNative(folder);

            IReadOnlyList<string> packages = Searcher.FindMatchingPackages(folder);

            if (packages.Count == 0)
            {
                Console.WriteLine("ERROR: folder does not contain any packages");

                return ExitCodes.ERROR;
            }

            if (string.IsNullOrEmpty(source))
            {
                Console.WriteLine("ERROR: source not specified");

                return ExitCodes.ERROR;
            }

            string apiKey = configuration.GetValue<string>(key: @"api-key")!;

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("ERROR: api-key not specified");

                return ExitCodes.ERROR;
            }

            IServiceProvider serviceProvider = ServiceConfiguration.Configure();

            IPusher pusher = serviceProvider.GetRequiredService<IPusher>();

            bool result = await pusher.PushAllAsync(source: source, symbolSource: symbolSource, packages: packages, apiKey: apiKey, cancellationToken: CancellationToken.None);

            return result
                ? ExitCodes.SUCCESS
                : ExitCodes.ERROR;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            return ExitCodes.ERROR;
        }
    }
}