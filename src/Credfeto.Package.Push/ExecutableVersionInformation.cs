using System;
using System.Diagnostics;

namespace Credfeto.Package.Push
{
    internal static class ExecutableVersionInformation
    {
        public static string ProgramVersion()
        {
            return CommonVersion(typeof(ExecutableVersionInformation));
        }

        private static string CommonVersion(Type type)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(type.Assembly.Location);

            return fileVersionInfo.ProductVersion!;
        }
    }
}