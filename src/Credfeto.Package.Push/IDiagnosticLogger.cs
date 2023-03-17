using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push;

/// <summary>
///     Diagnostic logger.
/// </summary>
public interface IDiagnosticLogger : ILogger
{
    /// <summary>
    ///     The number of errors encountered.
    /// </summary>
    long Errors { get; }

    /// <summary>
    ///     Whether any errors occured.
    /// </summary>
    bool IsErrored { get; }
}