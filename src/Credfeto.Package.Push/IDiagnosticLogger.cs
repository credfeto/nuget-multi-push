using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Push;

public interface IDiagnosticLogger : ILogger
{
    long Errors { get; }

    bool IsErrored { get; }
}