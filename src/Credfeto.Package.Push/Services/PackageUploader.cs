using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
    private const int MAX_RETRIES = 5;
    private readonly ILogger<PackageUploader> _logger;
    private readonly ILogger _nugetLogger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public PackageUploader(ILogger nugetLogger, ILogger<PackageUploader> logger)
    {
        this._nugetLogger = nugetLogger;
        this._logger = logger;
        this._retryPolicy = Policy.Handle((Func<Exception, bool>)IsTransientException)
                                  .WaitAndRetryAsync(retryCount: MAX_RETRIES,
                                                     sleepDurationProvider: CalculateRetry,
                                                     onRetry: (exception, delay, retryCount, context) => this._logger.TransientException(typeName: exception.GetType()
                                                                      .Name,
                                                                  retryCount: retryCount,
                                                                  maxRetries: MAX_RETRIES,
                                                                  delay: delay,
                                                                  $"{context.OperationKey}: {exception.GetType().FullName ?? "??"}: {exception.Message}",
                                                                  exception: exception));
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

            List<string> packagePaths = [package];

            int attempt = 0;

            await this._retryPolicy.ExecuteAsync(() =>
                                                 {
                                                     ++attempt;

                                                     return this.UploadOneAsync(packageUpdateResource: packageUpdateResource,
                                                                                apiKey: apiKey,
                                                                                symbolPackageUpdateResource: symbolPackageUpdateResource,
                                                                                packagePaths: packagePaths,
                                                                                attempt: attempt,
                                                                                symbolSource: symbolSource);
                                                 });

            return (package, success: true);
        }
        catch (Exception exception)
        {
            this._logger.FailedToUploadPackage(package: package,
                                               exception.GetType()
                                                        .FullName ?? "??",
                                               message: exception.Message,
                                               exception: exception);

            return (package, success: false);
        }
    }

    private static TimeSpan CalculateRetry(int retry)
    {
        return RetryDelayCalculator.CalculateWithJitter(attempts: retry, maxJitterSeconds: 10);
    }

    private Task UploadOneAsync(PackageUpdateResource packageUpdateResource,
                                string apiKey,
                                SymbolPackageUpdateResourceV3? symbolPackageUpdateResource,
                                List<string> packagePaths,
                                int attempt,
                                string? symbolSource)
    {
        foreach (string filename in packagePaths)
        {
            this._logger.UploadingPackage(filename: filename, attempt: attempt, maxRetries: MAX_RETRIES);
        }

        return packageUpdateResource.Push(packagePaths: packagePaths,
                                          symbolSource: symbolSource,
                                          timeoutInSecond: 800,
                                          disableBuffering: false,
                                          getApiKey: _ => apiKey,
                                          getSymbolApiKey: _ => apiKey,
                                          noServiceEndpoint: false,
                                          skipDuplicate: true,
                                          symbolPackageUpdateResource: symbolPackageUpdateResource,
                                          log: this._nugetLogger);
    }

    private static bool IsTransientException(Exception exception)
    {
        return exception is IOException or OperationCanceledException or TimeoutException or TaskCanceledException or HttpRequestException;
    }
}