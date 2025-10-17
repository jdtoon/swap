using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Extension methods for setting HTMX response headers in controller actions.
/// </summary>
public static class HtmxResponseExtensions
{
    // HTMX Response Header Constants
    private const string HX_LOCATION = "HX-Location";
    private const string HX_PUSH_URL = "HX-Push-Url";
    private const string HX_REDIRECT = "HX-Redirect";
    private const string HX_REFRESH = "HX-Refresh";
    private const string HX_REPLACE_URL = "HX-Replace-Url";
    private const string HX_RESWAP = "HX-Reswap";
    private const string HX_RETARGET = "HX-Retarget";
    private const string HX_RESELECT = "HX-Reselect";
    private const string HX_TRIGGER = "HX-Trigger";
    private const string HX_TRIGGER_AFTER_SETTLE = "HX-Trigger-After-Settle";
    private const string HX_TRIGGER_AFTER_SWAP = "HX-Trigger-After-Swap";

    /// <summary>
    /// Triggers a client-side redirect to the specified URL.
    /// </summary>
    public static void HxRedirect(this Controller controller, string url)
    {
        controller.Response.Headers[HX_REDIRECT] = url;
    }

    /// <summary>
    /// Triggers a full page refresh on the client.
    /// </summary>
    public static void HxRefresh(this Controller controller)
    {
        controller.Response.Headers[HX_REFRESH] = "true";
    }

    /// <summary>
    /// Pushes a new URL into the browser's history stack.
    /// </summary>
    public static void HxPushUrl(this Controller controller, string url)
    {
        controller.Response.Headers[HX_PUSH_URL] = url;
    }

    /// <summary>
    /// Replaces the current URL in the browser's location bar.
    /// </summary>
    public static void HxReplaceUrl(this Controller controller, string url)
    {
        controller.Response.Headers[HX_REPLACE_URL] = url;
    }

    /// <summary>
    /// Tells HTMX to do a client-side redirect with additional context.
    /// </summary>
    public static void HxLocation(this Controller controller, HtmxLocation location)
    {
        controller.Response.Headers[HX_LOCATION] = location.ToJson();
    }

    /// <summary>
    /// Specifies how the response will be swapped. See HtmxSwap enum for options.
    /// </summary>
    public static void HxReswap(this Controller controller, string swapStyle)
    {
        controller.Response.Headers[HX_RESWAP] = swapStyle;
    }

    /// <summary>
    /// Updates the target of the content update to a different element on the page.
    /// </summary>
    public static void HxRetarget(this Controller controller, string cssSelector)
    {
        controller.Response.Headers[HX_RETARGET] = cssSelector;
    }

    /// <summary>
    /// Updates the selector that is used to choose which part of the response is used to swap.
    /// </summary>
    public static void HxReselect(this Controller controller, string cssSelector)
    {
        controller.Response.Headers[HX_RESELECT] = cssSelector;
    }

    /// <summary>
    /// Triggers an event as soon as the response is received.
    /// </summary>
    public static void HxTrigger(this Controller controller, string eventName)
    {
        controller.Response.Headers[HX_TRIGGER] = eventName;
    }

    /// <summary>
    /// Triggers an event with a payload as soon as the response is received.
    /// </summary>
    public static void HxTrigger(this Controller controller, string eventName, object eventData)
    {
        var payload = new Dictionary<string, object>
        {
            [eventName] = eventData
        };
        controller.Response.Headers[HX_TRIGGER] = JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Triggers multiple events as soon as the response is received.
    /// </summary>
    public static void HxTrigger(this Controller controller, params string[] eventNames)
    {
        controller.Response.Headers[HX_TRIGGER] = string.Join(", ", eventNames);
    }

    /// <summary>
    /// Triggers an event after the settling step (after content swap and before next swap).
    /// </summary>
    public static void HxTriggerAfterSettle(this Controller controller, string eventName)
    {
        controller.Response.Headers[HX_TRIGGER_AFTER_SETTLE] = eventName;
    }

    /// <summary>
    /// Triggers an event with a payload after the settling step.
    /// </summary>
    public static void HxTriggerAfterSettle(this Controller controller, string eventName, object eventData)
    {
        var payload = new Dictionary<string, object>
        {
            [eventName] = eventData
        };
        controller.Response.Headers[HX_TRIGGER_AFTER_SETTLE] = JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Triggers an event after the swap step (immediately after swap, before settle).
    /// </summary>
    public static void HxTriggerAfterSwap(this Controller controller, string eventName)
    {
        controller.Response.Headers[HX_TRIGGER_AFTER_SWAP] = eventName;
    }

    /// <summary>
    /// Triggers an event with a payload after the swap step.
    /// </summary>
    public static void HxTriggerAfterSwap(this Controller controller, string eventName, object eventData)
    {
        var payload = new Dictionary<string, object>
        {
            [eventName] = eventData
        };
        controller.Response.Headers[HX_TRIGGER_AFTER_SWAP] = JsonSerializer.Serialize(payload);
    }
}
