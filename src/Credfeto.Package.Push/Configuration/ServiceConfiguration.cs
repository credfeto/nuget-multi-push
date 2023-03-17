using System;
using Credfeto.Package.Push.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push.Configuration;

internal static class ServiceConfiguration
{
    public static IServiceProvider Configure()
    {
        DiagnosticLogger logger = new(true);

        return new ServiceCollection().AddSingleton<ILogger>(logger)
                                      .AddSingleton<IDiagnosticLogger>(logger)
                                      .AddSingleton(typeof(ILogger<>), typeof(LoggerProxy<>))
                                      .AddSingleton<IPusher, Pusher>()
                                      .AddSingleton<IUploader, Uploader>()
                                      .BuildServiceProvider();
    }
}