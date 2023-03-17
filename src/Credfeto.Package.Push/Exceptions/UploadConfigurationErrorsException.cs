using System;

namespace Credfeto.Package.Push.Exceptions;

public sealed class UploadConfigurationErrorsException : Exception
{
    public UploadConfigurationErrorsException()
        : this(message: "Configuration errors were encountered.")
    {
    }

    public UploadConfigurationErrorsException(string message)
        : base(message)
    {
    }

    public UploadConfigurationErrorsException(string message, Exception innerException)
        : base(message: message, innerException: innerException)
    {
    }
}