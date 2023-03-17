using System;

namespace Credfeto.Package.Push.Exceptions;

public sealed class ConfigurationErrorsException : Exception
{
    public ConfigurationErrorsException()
        : this(message: "Configuration errors were encountered.")
    {
    }

    public ConfigurationErrorsException(string message)
        : base(message)
    {
    }

    public ConfigurationErrorsException(string message, Exception innerException)
        : base(message: message, innerException: innerException)
    {
    }
}