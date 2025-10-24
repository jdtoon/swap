namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Constants for HTMX response headers.
/// </summary>
public static class HtmxResponseHeaders
{
    /// <summary>
    /// Triggers an event on the client side.
    /// </summary>
    public const string Trigger = "HX-Trigger";
    
    /// <summary>
    /// Triggers an event after the settle phase.
    /// </summary>
    public const string TriggerAfterSettle = "HX-Trigger-After-Settle";
    
    /// <summary>
    /// Triggers an event after the swap phase.
    /// </summary>
    public const string TriggerAfterSwap = "HX-Trigger-After-Swap";
    
    /// <summary>
    /// Changes how the response will be swapped.
    /// </summary>
    public const string Reswap = "HX-Reswap";
    
    /// <summary>
    /// Changes the target element for the swap.
    /// </summary>
    public const string Retarget = "HX-Retarget";
    
    /// <summary>
    /// Performs a client-side redirect.
    /// </summary>
    public const string Redirect = "HX-Redirect";
    
    /// <summary>
    /// Refreshes the page.
    /// </summary>
    public const string Refresh = "HX-Refresh";
}