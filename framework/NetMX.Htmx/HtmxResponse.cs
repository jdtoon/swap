using Microsoft.AspNetCore.Mvc;

namespace NetMX.Htmx;

/// <summary>
/// Strongly-typed helper methods for working with HTMX responses.
/// Use these in your controllers to set HTMX response headers.
/// </summary>
public static class HtmxResponse
{
    /// <summary>
    /// Triggers a client-side event after the swap step is complete.
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="eventName">The name of the event to trigger</param>
    /// <param name="detail">Optional event detail object (will be JSON serialized)</param>
    public static void Trigger(Controller controller, string eventName, object? detail = null)
    {
        var headerValue = detail == null 
            ? eventName 
            : $"{{ \"{eventName}\": {System.Text.Json.JsonSerializer.Serialize(detail)} }}";
        
        controller.Response.Headers["HX-Trigger"] = headerValue;
    }

    /// <summary>
    /// Triggers a client-side event after the settle step is complete.
    /// </summary>
    public static void TriggerAfterSettle(Controller controller, string eventName, object? detail = null)
    {
        var headerValue = detail == null 
            ? eventName 
            : $"{{ \"{eventName}\": {System.Text.Json.JsonSerializer.Serialize(detail)} }}";
        
        controller.Response.Headers["HX-Trigger-After-Settle"] = headerValue;
    }

    /// <summary>
    /// Triggers a client-side event after the swap step is complete.
    /// </summary>
    public static void TriggerAfterSwap(Controller controller, string eventName, object? detail = null)
    {
        var headerValue = detail == null 
            ? eventName 
            : $"{{ \"{eventName}\": {System.Text.Json.JsonSerializer.Serialize(detail)} }}";
        
        controller.Response.Headers["HX-Trigger-After-Swap"] = headerValue;
    }

    /// <summary>
    /// Pushes a new URL into the browser's history stack.
    /// </summary>
    public static void PushUrl(Controller controller, string url)
    {
        controller.Response.Headers["HX-Push-Url"] = url;
    }

    /// <summary>
    /// Replaces the current URL in the browser's history stack.
    /// </summary>
    public static void ReplaceUrl(Controller controller, string url)
    {
        controller.Response.Headers["HX-Replace-Url"] = url;
    }

    /// <summary>
    /// Allows you to specify a different target element to swap into.
    /// </summary>
    public static void Retarget(Controller controller, string cssSelector)
    {
        controller.Response.Headers["HX-Retarget"] = cssSelector;
    }

    /// <summary>
    /// Allows you to specify a different swap strategy.
    /// </summary>
    public static void Reswap(Controller controller, HtmxSwap swap)
    {
        controller.Response.Headers["HX-Reswap"] = swap.ToHtmxValue();
    }

    /// <summary>
    /// Triggers a client-side redirect.
    /// </summary>
    public static void Redirect(Controller controller, string url)
    {
        controller.Response.Headers["HX-Redirect"] = url;
    }

    /// <summary>
    /// Triggers a full page refresh.
    /// </summary>
    public static void Refresh(Controller controller)
    {
        controller.Response.Headers["HX-Refresh"] = "true";
    }
}
