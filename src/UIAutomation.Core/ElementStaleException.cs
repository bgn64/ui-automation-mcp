namespace UIAutomation.Core;

/// <summary>
/// Indicates that a cached UI element is no longer available from the host accessibility system.
/// </summary>
public sealed class ElementStaleException : InvalidOperationException
{
    public ElementStaleException(string message)
        : base(message)
    {
    }

    public ElementStaleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
