namespace Swap.Testing;

/// <summary>
/// Exception thrown when HTMX test assertions fail.
/// </summary>
public class HtmxTestException : Exception
{
    public HtmxTestException(string message) : base(message)
    {
    }

    public HtmxTestException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
