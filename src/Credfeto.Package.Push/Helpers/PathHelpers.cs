using System.IO;

namespace Credfeto.Package.Push.Helpers;

internal static class PathHelpers
{
    public static string ConvertToNative(string path)
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            return path.Replace(oldChar: '/', newChar: Path.DirectorySeparatorChar);
        }

        return path.Replace(oldChar: '\\', newChar: Path.DirectorySeparatorChar);
    }
}