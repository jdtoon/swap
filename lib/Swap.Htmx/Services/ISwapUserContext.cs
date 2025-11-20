using Microsoft.AspNetCore.Http;

namespace Swap.Htmx.Services;

/// <summary>
/// Abstraction for retrieving the current user's session ID or unique identifier.
/// </summary>
public interface ISwapUserContext
{
    /// <summary>
    /// Gets the current session ID.
    /// </summary>
    string GetSessionId();
}
