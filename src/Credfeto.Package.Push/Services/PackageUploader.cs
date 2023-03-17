using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Credfeto.Package.Push.Extensions;
using Credfeto.Package.Push.Helpers;
using Credfeto.Package.Push.Services.LoggingExtensions;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;
using Polly;
using Polly.Retry;
using ILogger = NuGet.Common.ILogger;

namespace Credfeto.Package.Push.Services;

public sealed class PackageUploader : IPackageUploader
{
    private const int MAX_RETRIES = 3;
    private static readonly ILogger NugetLogger = new ConsoleLogger();
    private readonly ILogger<PackageUploader> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public PackageUploader(ILogger<PackageUploader> logger)
    {
        this._logger = logger;
        this._retryPolicy = Policy.Handle((Func<Exception, bool>)IsTransientException)
                                  .WaitAndRetryAsync(retryCount: MAX_RETRIES,
                                                     sleepDurationProvider: RetryDelayCalculator.Calculate,
                                                     onRetry: (exception, delay, retryCount, context) =>
                                                              {
                                                                  this._logger.LogAndDispatchTransientException(typeName: exception.GetType()
                                                                                                                                   .Name,
                                                                                                                retryCount: retryCount,
                                                                                                                maxRetries: MAX_RETRIES,
                                                                                                                delay: delay,
                                                                                                                $"{context.OperationKey}: {exception.Message}",
                                                                                                                exception: exception);
                                                              });
    }

    public async Task<(string package, bool success)> PushOnePackageAsync(string package,
                                                                          IReadOnlyList<string> symbolPackages,
                                                                          PackageUpdateResource packageUpdateResource,
                                                                          string apiKey,
                                                                          SymbolPackageUpdateResourceV3? symbolPackageUpdateResource)
    {
        try
        {
            string? symbolSource = symbolPackages.FindMatchingSymbolPackage(package: package, logger: this._logger);

            List<string> packagePaths = new() { package };

            await this._retryPolicy.ExecuteAsync(() => packageUpdateResource.Push(packagePaths: packagePaths,
                                                                                  symbolSource: symbolSource,
                                                                                  timeoutInSecond: 800,
                                                                                  disableBuffering: false,
                                                                                  getApiKey: _ => apiKey,
                                                                                  getSymbolApiKey: _ => apiKey,
                                                                                  noServiceEndpoint: false,
                                                                                  skipDuplicate: true,
                                                                                  symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                                                  log: NugetLogger));

            return (package, success: true);
        }
        catch (Exception exception)
        {
            this._logger.LogError($"ERROR: Failed to upload {package}: {exception.Message}");

            return (package, success: false);
        }
    }

    private static bool IsTransientException(Exception exception)
    {
        return exception is IOException or OperationCanceledException or TimeoutException or TaskCanceledException;
    }
}