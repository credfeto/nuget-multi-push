using System.Collections.Generic;
using System.IO;
using System.Linq;
using Credfeto.Package.Push.Constants;

namespace Credfeto.Package.Push;

public static class Searcher
{
    public static IReadOnlyList<string> FindMatchingPackages(string folder)
    {
        return Directory.GetFiles(path: folder, searchPattern: PackageNaming.SearchPattern)
                        .Concat(Directory.GetFiles(path: folder, searchPattern: PackageNaming.SourceSearchPattern))
                        .ToArray();
    }
}