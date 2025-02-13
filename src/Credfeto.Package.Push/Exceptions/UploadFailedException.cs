using System;

namespace Credfeto.Package.Push.Exceptions;

public sealed class UploadFailedException : Exception
{
    public UploadFailedException()
        : this(message: "Upload failed.") { }

    public UploadFailedException(string message)
        : base(message) { }

    public UploadFailedException(string message, Exception innerException)
        : base(message: message, innerException: innerException) { }
}
