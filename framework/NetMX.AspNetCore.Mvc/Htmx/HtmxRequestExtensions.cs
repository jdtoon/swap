using Microsoft.AspNetCore.Http;

namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Extension methods for HttpRequest to detect and read HTMX request headers.
/// </summary>
public static class HtmxRequestExtensions
{
    // HTMX Request Header Constants
    private const string HX_REQUEST = "HX-Request";
    private const string HX_BOOSTED = "HX-Boosted";
    private const string HX_CURRENT_URL = "HX-Current-URL";
    private const string HX_HISTORY_RESTORE_REQUEST = "HX-History-Restore-Request";
    private const string HX_PROMPT = "HX-Prompt";
    private const string HX_TARGET = "HX-Target";
    private const string HX_TRIGGER_NAME = "HX-Trigger-Name";
    private const string HX_TRIGGER = "HX-Trigger";

    /// <summary>
    /// Indicates that the request is an HTMX request.
    /// Always true when the HX-Request header is present.
    /// </summary>
    public static bool IsHtmx(this HttpRequest request)
    {
        return request.Headers.ContainsKey(HX_REQUEST);
    }

    /// <summary>
    /// Indicates that the request is via an element using hx-boost.
    /// </summary>
    public static bool IsBoosted(this HttpRequest request)
    {
        return request.Headers.ContainsKey(HX_BOOSTED) &&
               request.Headers[HX_BOOSTED] == "true";
    }

    /// <summary>
    /// Indicates that the request is for history restoration after a miss in the local history cache.
    /// </summary>
    public static bool IsHistoryRestoreRequest(this HttpRequest request)
    {
        return request.Headers.ContainsKey(HX_HISTORY_RESTORE_REQUEST) &&
               request.Headers[HX_HISTORY_RESTORE_REQUEST] == "true";
    }

    /// <summary>
    /// Gets the current URL of the browser when the request was made.
    /// </summary>
    public static string? GetCurrentUrl(this HttpRequest request)
    {
        return request.Headers.TryGetValue(HX_CURRENT_URL, out var value)
            ? value.ToString()
            : null;
    }

    /// <summary>
    /// Gets the user response to an hx-prompt, if present.
    /// </summary>
    public static string? GetPrompt(this HttpRequest request)
    {
        return request.Headers.TryGetValue(HX_PROMPT, out var value)
            ? value.ToString()
            : null;
    }

    /// <summary>
    /// Gets the id of the target element if it exists.
    /// </summary>
    public static string? GetTarget(this HttpRequest request)
    {
        return request.Headers.TryGetValue(HX_TARGET, out var value)
            ? value.ToString()
            : null;
    }

    /// <summary>
    /// Gets the name of the triggered element if it exists.
    /// </summary>
    public static string? GetTriggerName(this HttpRequest request)
    {
        return request.Headers.TryGetValue(HX_TRIGGER_NAME, out var value)
            ? value.ToString()
            : null;
    }

    /// <summary>
    /// Gets the id of the triggered element if it exists.
    /// </summary>
    public static string? GetTriggerId(this HttpRequest request)
    {
        return request.Headers.TryGetValue(HX_TRIGGER, out var value)
            ? value.ToString()
            : null;
    }
}
