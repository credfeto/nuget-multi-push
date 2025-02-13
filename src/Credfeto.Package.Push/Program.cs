using System;
using System.Threading;
using System.Threading.Tasks;
using Cocona;
using Cocona.Builder;
using Credfeto.Package.Push.Configuration;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Exceptions;
using Credfeto.Package.Push.Helpers;

namespace Credfeto.Package.Push;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"{VersionInformation.Product} {VersionInformation.Version}");

        try
        {
            CoconaAppBuilder builder = CoconaApp.CreateBuilder(args);
            builder.Services.AddServices();

            CoconaApp app = builder.Build();
            app.AddCommands<Commands>();

            await app.RunAsync(CancellationToken.None);

            return ExitCodes.Success;
        }
        catch (UploadFailedException)
        {
            return ExitCodes.Error;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            return ExitCodes.Error;
        }
    }
}
