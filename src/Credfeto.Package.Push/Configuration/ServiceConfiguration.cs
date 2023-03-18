using System;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Configuration;

internal static class ServiceConfiguration
{
    public static IServiceProvider Configure(bool warningsAsErrors)
    {
        DiagnosticLogger logger = new(warningsAsErrors);

        return new ServiceCollection().AddSingleton<ILogger>(logger)
                                      .AddSingleton<IDiagnosticLogger>(logger)
                                      .AddSingleton(typeof(ILogger<>), typeof(LoggerProxy<>))
                                      .AddSingleton<IUploadOrchestration, UploadOrchestration>()
                                      .AddSingleton<IPackageUploader, PackageUploader>()
                                      .BuildServiceProvider();
    }
}