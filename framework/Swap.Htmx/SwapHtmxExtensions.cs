using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System;
using Swap.Htmx.Models;

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
        return request.Headers.ContainsKey(HxHeaders.Request);
    }

    /// <summary>
    /// Checks if the current request is a boosted HTMX request (contains HX-Boosted header).
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>True if the request has the HX-Boosted header, false otherwise.</returns>
    public static bool IsHtmxBoosted(this HttpRequest request)
    {
        return request.Headers.ContainsKey(HxHeaders.Boosted);
    }

    /// <summary>
    /// Gets the current URL from the HX-Current-URL header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The current URL if present, null otherwise.</returns>
    public static string? GetHtmxCurrentUrl(this HttpRequest request)
    {
        return request.Headers[HxHeaders.CurrentUrl].FirstOrDefault();
    }

    /// <summary>
    /// Gets the current URL from the HX-Current-URL header as a Uri.
    /// </summary>
    public static Uri? GetHtmxCurrentUrlUri(this HttpRequest request)
    {
        var url = request.GetHtmxCurrentUrl();
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;
    }

    /// <summary>
    /// Gets the ID of the target element from the HX-Target header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The target element ID if present, null otherwise.</returns>
    public static string? GetHtmxTarget(this HttpRequest request)
    {
        return request.Headers[HxHeaders.Target].FirstOrDefault();
    }

    /// <summary>
    /// Gets the ID of the element that triggered the request from the HX-Trigger header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The trigger element ID if present, null otherwise.</returns>
    public static string? GetHtmxTrigger(this HttpRequest request)
    {
        return request.Headers[HxHeaders.Trigger].FirstOrDefault();
    }

    /// <summary>
    /// Gets the name attribute of the element that triggered the request (HX-Trigger-Name).
    /// </summary>
    public static string? GetHtmxTriggerName(this HttpRequest request)
    {
        return request.Headers[HxHeaders.TriggerName].FirstOrDefault();
    }

    /// <summary>
    /// Gets the user input from the htmx prompt dialog (HX-Prompt).
    /// </summary>
    public static string? GetHtmxPrompt(this HttpRequest request)
    {
        return request.Headers[HxHeaders.Prompt].FirstOrDefault();
    }

    /// <summary>
    /// Checks if this request is a history restore request (back/forward navigation) via HX-History-Restore-Request.
    /// </summary>
    public static bool IsHtmxHistoryRestoreRequest(this HttpRequest request)
    {
        var value = request.Headers[HxHeaders.HistoryRestore].FirstOrDefault();
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
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
        response.Headers[HxHeaders.TriggerResp] = eventName;
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
        response.Headers[HxHeaders.TriggerResp] = json;
    }

    /// <summary>
    /// Triggers a client event with a typed details object that will be serialized to JSON.
    /// Produces: { "eventName": { ...details... } }
    /// </summary>
    public static void HxTrigger(this HttpResponse response, string eventName, object details)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [eventName] = details
        });
        response.Headers[HxHeaders.TriggerResp] = payload;
    }

    /// <summary>
    /// Queue an event to fire on the client after swap completes.
    /// </summary>
    public static void HxTriggerAfterSwap(this HttpResponse response, string eventName)
    {
        response.Headers[HxHeaders.TriggerAfterSwap] = eventName;
    }

    /// <summary>
    /// Queue an event with JSON details to fire after swap completes.
    /// </summary>
    public static void HxTriggerAfterSwapWithDetails(this HttpResponse response, string json)
    {
        response.Headers[HxHeaders.TriggerAfterSwap] = json;
    }

    /// <summary>
    /// Queue a typed-details event to fire after swap completes.
    /// Produces: { "eventName": { ...details... } }
    /// </summary>
    public static void HxTriggerAfterSwap(this HttpResponse response, string eventName, object details)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [eventName] = details
        });
        response.Headers[HxHeaders.TriggerAfterSwap] = payload;
    }

    /// <summary>
    /// Queue an event to fire on the client after settle completes.
    /// </summary>
    public static void HxTriggerAfterSettle(this HttpResponse response, string eventName)
    {
        response.Headers[HxHeaders.TriggerAfterSettle] = eventName;
    }

    /// <summary>
    /// Queue an event with JSON details to fire after settle completes.
    /// </summary>
    public static void HxTriggerAfterSettleWithDetails(this HttpResponse response, string json)
    {
        response.Headers[HxHeaders.TriggerAfterSettle] = json;
    }

    /// <summary>
    /// Queue a typed-details event to fire after settle completes.
    /// Produces: { "eventName": { ...details... } }
    /// </summary>
    public static void HxTriggerAfterSettle(this HttpResponse response, string eventName, object details)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [eventName] = details
        });
        response.Headers[HxHeaders.TriggerAfterSettle] = payload;
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
        response.Headers[HxHeaders.PushUrl] = url;
    }

    /// <summary>
    /// Prevents the browser history from being updated.
    /// Sets the HX-Push-Url response header to "false".
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    public static void HxPreventPushUrl(this HttpResponse response)
    {
        response.Headers[HxHeaders.PushUrl] = "false";
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
        response.Headers[HxHeaders.ReplaceUrl] = url;
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
        response.Headers[HxHeaders.Redirect] = url;
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
        response.Headers[HxHeaders.Refresh] = "true";
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
        response.Headers[HxHeaders.Retarget] = selector;
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
        response.Headers[HxHeaders.Reswap] = swapStrategy;
    }

    /// <summary>
    /// Strongly-typed HX-Reswap using options builder.
    /// </summary>
    public static void HxReswap(this HttpResponse response, HxReswapOptions options)
    {
        response.Headers[HxHeaders.Reswap] = options.ToHeaderValue();
    }

    /// <summary>
    /// Client-side redirect without full page reload using HX-Location.
    /// Accepts either a URL string or a JSON object with advanced options per htmx docs.
    /// </summary>
    public static void HxLocation(this HttpResponse response, string url)
    {
        response.Headers[HxHeaders.Location] = url;
    }

    /// <summary>
    /// Client-side redirect without full page reload using HX-Location with an options object.
    /// Example: new { path = "/inbox", target = "#main", swap = "innerHTML" }
    /// </summary>
    public static void HxLocation(this HttpResponse response, object options)
    {
        response.Headers[HxHeaders.Location] = JsonSerializer.Serialize(options);
    }

    /// <summary>
    /// Strongly-typed HX-Location JSON body.
    /// </summary>
    public static void HxLocation(this HttpResponse response, HxLocationOptions options)
    {
        response.Headers[HxHeaders.Location] = JsonSerializer.Serialize(options);
    }

    /// <summary>
    /// Select a sub-fragment from the response HTML using a CSS selector.
    /// Sets the HX-Reselect response header.
    /// </summary>
    public static void HxReselect(this HttpResponse response, string selector)
    {
        response.Headers[HxHeaders.Reselect] = selector;
    }

    /// <summary>
    /// Signal the client to stop polling this endpoint by returning HTTP status 286.
    /// </summary>
    public static void HxStopPolling(this HttpResponse response)
    {
        response.StatusCode = 286; // htmx polling cancel
    }

    /// <summary>
    /// Ensure the response varies on the HX-Request header when content differs for HTMX vs full requests.
    /// Appends/sets Vary: HX-Request appropriately.
    /// </summary>
    public static void EnsureVaryHxRequest(this HttpResponse response)
    {
        const string vary = "Vary";
        var existing = response.Headers[vary].ToString();
        if (string.IsNullOrWhiteSpace(existing))
        {
            response.Headers[vary] = HxHeaders.Request;
            return;
        }

        // Avoid duplicate entries
        var parts = existing.Split(',');
        foreach (var p in parts)
        {
            if (string.Equals(p.Trim(), HxHeaders.Request, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
        response.Headers[vary] = existing + ", " + HxHeaders.Request;
    }
}
