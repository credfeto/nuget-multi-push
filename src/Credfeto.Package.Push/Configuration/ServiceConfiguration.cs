using System;
using Credfeto.Package.Push.Helpers;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = NuGet.Common.ILogger;

namespace Credfeto.Package.Push.Configuration;

internal static class ServiceConfiguration
{
    public static IServiceProvider Configure(bool warningsAsErrors)
    {
        DiagnosticLogger logger = new(warningsAsErrors);

        return new ServiceCollection().BuildServiceProvider();
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        return services.AddSingleton<ILogger, NugetForwardingLogger>()
                       .AddSingleton(typeof(ILogger<>), typeof(LoggerProxy<>))
                       .AddSingleton<IUploadOrchestration, UploadOrchestration>()
                       .AddSingleton<IPackageUploader, PackageUploader>();
    }
}