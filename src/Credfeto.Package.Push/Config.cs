using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Credfeto.Package.Push;

public static class Config
{
    public static IConfigurationRoot LoadConfiguration(string[] args)
    {
        return new ConfigurationBuilder().AddCommandLine(args: args,
                                                         new Dictionary<string, string>(StringComparer.Ordinal)
                                                         {
                                                             { @"-folder", @"folder" }, { @"-source", @"source" }, { @"-symbol-source", @"symbol-source" }, { @"-api-key", @"api-key" }
                                                         })
                                         .Build();
    }
}