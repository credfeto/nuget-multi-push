using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace Credfeto.Package.Push;

public interface IPackageUploader
{
    Task<(string package, bool success)> PushOnePackageAsync(string package,
                                                             IReadOnlyList<string> symbolPackages,
                                                             PackageUpdateResource packageUpdateResource,
                                                             string apiKey,
                                                             SymbolPackageUpdateResourceV3? symbolPackageUpdateResource);
}