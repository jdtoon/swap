using Swap.Htmx.Models;

namespace Swap.Htmx;

/// <summary>
/// Static entry point for Minimal APIs.
/// </summary>
public static class SwapResults
{
    /// <summary>
    /// Creates a fluent response builder for coordinating multiple updates in a single response.
    /// </summary>
    /// <returns>A fluent builder for constructing coordinated HTMX responses.</returns>
    public static SwapResponseBuilder Response()
    {
        return new SwapResponseBuilder();
    }
}
