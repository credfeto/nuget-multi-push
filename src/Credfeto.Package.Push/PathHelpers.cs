using System.IO;

namespace Credfeto.Package.Push;

/// <summary>
///     Helpers for working with paths
/// </summary>
public static class PathHelpers
{
    /// <summary>
    ///     Converts the path to the native format.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The native format path.</returns>
    public static string ConvertToNative(string path)
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            return path.Replace(oldChar: '/', newChar: Path.DirectorySeparatorChar);
        }

        return path.Replace(oldChar: '\\', newChar: Path.DirectorySeparatorChar);
    }
}