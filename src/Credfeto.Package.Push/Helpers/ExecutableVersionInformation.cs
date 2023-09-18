namespace Credfeto.Package.Push.Helpers;

internal static class ExecutableVersionInformation
{
    public static string ProgramVersion()
    {
        return ThisAssembly.Info.FileVersion;
    }
}