using Credfeto.Package.Push.Helpers;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.DependencyInjection;
using ILogger = NuGet.Common.ILogger;

namespace Credfeto.Package.Push.Configuration;

internal static class ServiceConfiguration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        return services.AddSingleton<ILogger, NugetForwardingLogger>()
                       .AddSingleton<IUploadOrchestration, UploadOrchestration>()
                       .AddSingleton<IPackageUploader, PackageUploader>();
    }
}