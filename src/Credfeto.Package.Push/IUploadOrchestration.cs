using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Package.Push;

public interface IUploadOrchestration
{
    Task<bool> PushAllAsync(string source, string symbolSource, IReadOnlyList<string> packages, string apiKey, CancellationToken cancellationToken);
}