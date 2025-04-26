using System.Collections.Generic;
using System.IO;
using System.Linq;
using Credfeto.Package.Push.Constants;
using Credfeto.Package.Push.Helpers;

namespace Credfeto.Package.Push;

public static class Searcher
{
    public static IReadOnlyList<string> FindMatchingPackages(string folder)
    {
        string nativeFolder = PathHelpers.ConvertToNative(folder);

        return
        [
            .. Directory
                .GetFiles(path: nativeFolder, searchPattern: PackageNaming.SearchPattern)
                .Concat(Directory.GetFiles(path: nativeFolder, searchPattern: PackageNaming.SourceSearchPattern)),
        ];
    }
}
