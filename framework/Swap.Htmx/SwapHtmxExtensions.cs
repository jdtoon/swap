using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for working with HTMX requests and responses.
/// Provides helpers for setting HTMX response headers and detecting HTMX requests.
/// </summary>
public static class SwapHtmxExtensions
{
    /// <summary>
    /// Checks if the current request is an HTMX request (contains HX-Request header).
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>True if the request has the HX-Request header, false otherwise.</returns>
    public static bool IsHtmxRequest(this HttpRequest request)
    {
        return request.Headers.ContainsKey("HX-Request");
    }

    /// <summary>
    /// Checks if the current request is a boosted HTMX request (contains HX-Boosted header).
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>True if the request has the HX-Boosted header, false otherwise.</returns>
    public static bool IsHtmxBoosted(this HttpRequest request)
    {
        return request.Headers.ContainsKey("HX-Boosted");
    }

    /// <summary>
    /// Gets the current URL from the HX-Current-URL header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The current URL if present, null otherwise.</returns>
    public static string? GetHtmxCurrentUrl(this HttpRequest request)
    {
        return request.Headers["HX-Current-URL"].FirstOrDefault();
    }

    /// <summary>
    /// Gets the ID of the target element from the HX-Target header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The target element ID if present, null otherwise.</returns>
    public static string? GetHtmxTarget(this HttpRequest request)
    {
        return request.Headers["HX-Target"].FirstOrDefault();
    }

    /// <summary>
    /// Gets the ID of the element that triggered the request from the HX-Trigger header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The trigger element ID if present, null otherwise.</returns>
    public static string? GetHtmxTrigger(this HttpRequest request)
    {
        return request.Headers["HX-Trigger"].FirstOrDefault();
    }

    /// <summary>
    /// Triggers an event on the client side after the response is processed.
    /// Sets the HX-Trigger response header.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="eventName">The name of the event to trigger.</param>
    /// <example>
    /// <code>
    /// Response.HxTrigger("itemCreated");
    /// </code>
    /// </example>
    public static void HxTrigger(this HttpResponse response, string eventName)
    {
        response.Headers["HX-Trigger"] = eventName;
    }

    /// <summary>
    /// Triggers an event on the client side after the response is processed with JSON details.
    /// Sets the HX-Trigger response header with JSON payload.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="json">The JSON string containing event details.</param>
    /// <example>
    /// <code>
    /// Response.HxTrigger("{\"showMessage\": {\"level\": \"info\", \"message\": \"Item saved\"}}");
    /// </code>
    /// </example>
    public static void HxTriggerWithDetails(this HttpResponse response, string json)
    {
        response.Headers["HX-Trigger"] = json;
    }

    /// <summary>
    /// Pushes a new URL to the browser history without reloading the page.
    /// Sets the HX-Push-Url response header.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="url">The URL to push to the browser history.</param>
    /// <example>
    /// <code>
    /// Response.HxPushUrl($"/articles/{article.Id}");
    /// </code>
    /// </example>
    public static void HxPushUrl(this HttpResponse response, string url)
    {
        response.Headers["HX-Push-Url"] = url;
    }

    /// <summary>
    /// Prevents the browser history from being updated.
    /// Sets the HX-Push-Url response header to "false".
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    public static void HxPreventPushUrl(this HttpResponse response)
    {
        response.Headers["HX-Push-Url"] = "false";
    }

    /// <summary>
    /// Replaces the current URL in the browser history.
    /// Sets the HX-Replace-Url response header.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="url">The URL to replace in the browser history.</param>
    /// <example>
    /// <code>
    /// Response.HxReplaceUrl($"/articles/{article.Id}");
    /// </code>
    /// </example>
    public static void HxReplaceUrl(this HttpResponse response, string url)
    {
        response.Headers["HX-Replace-Url"] = url;
    }

    /// <summary>
    /// Performs a client-side redirect.
    /// Sets the HX-Redirect response header.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="url">The URL to redirect to.</param>
    /// <example>
    /// <code>
    /// Response.HxRedirect("/login");
    /// </code>
    /// </example>
    public static void HxRedirect(this HttpResponse response, string url)
    {
        response.Headers["HX-Redirect"] = url;
    }

    /// <summary>
    /// Forces a full page refresh.
    /// Sets the HX-Refresh response header to "true".
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <example>
    /// <code>
    /// Response.HxRefresh();
    /// </code>
    /// </example>
    public static void HxRefresh(this HttpResponse response)
    {
        response.Headers["HX-Refresh"] = "true";
    }

    /// <summary>
    /// Changes the target element for the response content.
    /// Sets the HX-Retarget response header.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="selector">The CSS selector for the new target element.</param>
    /// <example>
    /// <code>
    /// Response.HxRetarget("#notification-area");
    /// </code>
    /// </example>
    public static void HxRetarget(this HttpResponse response, string selector)
    {
        response.Headers["HX-Retarget"] = selector;
    }

    /// <summary>
    /// Changes how the response content will be swapped into the target element.
    /// Sets the HX-Reswap response header.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="swapStrategy">The swap strategy (e.g., "innerHTML", "outerHTML", "beforebegin", "afterend").</param>
    /// <example>
    /// <code>
    /// Response.HxReswap("beforebegin");
    /// </code>
    /// </example>
    public static void HxReswap(this HttpResponse response, string swapStrategy)
    {
        response.Headers["HX-Reswap"] = swapStrategy;
    }
}
