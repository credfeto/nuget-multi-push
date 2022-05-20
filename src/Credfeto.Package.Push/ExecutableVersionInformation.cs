﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Credfeto.Package.Push;

internal static class ExecutableVersionInformation
{
    public static string ProgramVersion()
    {
        return CommonVersion(typeof(ExecutableVersionInformation));
    }

    private static string CommonVersion(Type type)
    {
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(GetFileName(type.Assembly));

        return fileVersionInfo.ProductVersion!;
    }

    [SuppressMessage(category: "FunFair.CodeAnalysis", checkId: "FFS0008:Don't disable warnings with #pragma", Justification = "Needed in this case")]
    private static string GetFileName(Assembly assembly)
    {
#pragma warning disable IL3000
        return assembly.Location;
#pragma warning restore IL3000
    }
}