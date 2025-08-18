using System;

namespace SpecialGuide.Core.Services;

public class ClipboardWriteException : Exception
{
    public ClipboardWriteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
