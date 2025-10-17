using Microsoft.AspNetCore.Http;

namespace NetMX.Htmx;

/// <summary>
/// Extension methods for HttpRequest to check for HTMX-specific headers.
/// </summary>
public static class HtmxRequestExtensions
{
    /// <summary>
    /// Returns true if the request was made by HTMX.
    /// </summary>
    public static bool IsHtmx(this HttpRequest request)
    {
        return request.Headers.ContainsKey("HX-Request");
    }

    /// <summary>
    /// Returns true if the request is for history restoration after a miss in the local history cache.
    /// </summary>
    public static bool IsHistoryRestoreRequest(this HttpRequest request)
    {
        return request.Headers.ContainsKey("HX-History-Restore-Request");
    }

    /// <summary>
    /// Gets the current URL of the browser when the HTMX request was made.
    /// </summary>
    public static string? GetCurrentUrl(this HttpRequest request)
    {
        return request.Headers.TryGetValue("HX-Current-URL", out var value) 
            ? value.ToString() 
            : null;
    }

    /// <summary>
    /// Gets the ID of the target element.
    /// </summary>
    public static string? GetTargetId(this HttpRequest request)
    {
        return request.Headers.TryGetValue("HX-Target", out var value) 
            ? value.ToString() 
            : null;
    }

    /// <summary>
    /// Gets the ID of the element that triggered the request.
    /// </summary>
    public static string? GetTriggerId(this HttpRequest request)
    {
        return request.Headers.TryGetValue("HX-Trigger", out var value) 
            ? value.ToString() 
            : null;
    }

    /// <summary>
    /// Gets the name of the triggered event.
    /// </summary>
    public static string? GetTriggerName(this HttpRequest request)
    {
        return request.Headers.TryGetValue("HX-Trigger-Name", out var value) 
            ? value.ToString() 
            : null;
    }

    /// <summary>
    /// Returns true if the request includes a prompt response.
    /// </summary>
    public static bool HasPrompt(this HttpRequest request)
    {
        return request.Headers.ContainsKey("HX-Prompt");
    }

    /// <summary>
    /// Gets the user's prompt response, if present.
    /// </summary>
    public static string? GetPromptResponse(this HttpRequest request)
    {
        return request.Headers.TryGetValue("HX-Prompt", out var value) 
            ? value.ToString() 
            : null;
    }
}
