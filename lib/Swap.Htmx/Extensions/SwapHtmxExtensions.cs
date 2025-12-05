using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System;
using Swap.Htmx.Filters;
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
    /// Checks if the layout should be suppressed for this request.
    /// Use this in _ViewStart.cshtml when AutoSuppressLayout is enabled:
    /// <code>
    /// @{ Layout = Context.ShouldSuppressLayout() ? null : "_Layout"; }
    /// </code>
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if layout should be suppressed (HTMX request, non-boosted).</returns>
    public static bool ShouldSuppressLayout(this HttpContext context)
    {
        return context.Items[SwapLayoutFilter.SuppressLayoutKey] is true;
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
        MergeTriggerHeader(response, HxHeaders.TriggerResp, eventName, null);
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
        MergeTriggerHeaderJson(response, HxHeaders.TriggerResp, json);
    }

    /// <summary>
    /// Triggers a client event with a typed details object that will be serialized to JSON.
    /// Produces: { "eventName": { ...details... } }
    /// </summary>
    public static void HxTrigger(this HttpResponse response, string eventName, object details)
    {
        MergeTriggerHeader(response, HxHeaders.TriggerResp, eventName, details);
    }

    /// <summary>
    /// Queue an event to fire on the client after swap completes.
    /// </summary>
    public static void HxTriggerAfterSwap(this HttpResponse response, string eventName)
    {
        MergeTriggerHeader(response, HxHeaders.TriggerAfterSwap, eventName, null);
    }

    /// <summary>
    /// Queue an event with JSON details to fire after swap completes.
    /// </summary>
    public static void HxTriggerAfterSwapWithDetails(this HttpResponse response, string json)
    {
        MergeTriggerHeaderJson(response, HxHeaders.TriggerAfterSwap, json);
    }

    /// <summary>
    /// Queue a typed-details event to fire after swap completes.
    /// Produces: { "eventName": { ...details... } }
    /// </summary>
    public static void HxTriggerAfterSwap(this HttpResponse response, string eventName, object details)
    {
        MergeTriggerHeader(response, HxHeaders.TriggerAfterSwap, eventName, details);
    }

    /// <summary>
    /// Queue an event to fire on the client after settle completes.
    /// </summary>
    public static void HxTriggerAfterSettle(this HttpResponse response, string eventName)
    {
        MergeTriggerHeader(response, HxHeaders.TriggerAfterSettle, eventName, null);
    }

    /// <summary>
    /// Queue an event with JSON details to fire after settle completes.
    /// </summary>
    public static void HxTriggerAfterSettleWithDetails(this HttpResponse response, string json)
    {
        MergeTriggerHeaderJson(response, HxHeaders.TriggerAfterSettle, json);
    }

    /// <summary>
    /// Queue a typed-details event to fire after settle completes.
    /// Produces: { "eventName": { ...details... } }
    /// </summary>
    public static void HxTriggerAfterSettle(this HttpResponse response, string eventName, object details)
    {
        MergeTriggerHeader(response, HxHeaders.TriggerAfterSettle, eventName, details);
    }

    /// <summary>
    /// Helper to merge a trigger event into an existing HX-Trigger* header.
    /// </summary>
    private static void MergeTriggerHeader(HttpResponse response, string headerName, string eventName, object? details)
    {
        if (response.Headers.ContainsKey(headerName))
        {
            var existing = response.Headers[headerName].ToString();

            if (existing.TrimStart().StartsWith("{"))
            {
                // Existing is JSON object - parse and merge
                try
                {
                    var existingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existing);
                    if (existingDict != null)
                    {
                        existingDict[eventName] = details ?? (object)"null";
                        response.Headers[headerName] = JsonSerializer.Serialize(existingDict);
                        return;
                    }
                }
                catch
                {
                    // If parsing fails, fall through to string manipulation
                }

                // Fallback: string manipulation
                existing = existing.TrimEnd().TrimEnd('}');
                var newEventJson = details == null
                    ? $"\"{eventName}\": null"
                    : $"\"{eventName}\": {JsonSerializer.Serialize(details)}";
                response.Headers[headerName] = $"{existing}, {newEventJson}}}";
            }
            else
            {
                // Existing is simple event name(s) - convert to JSON
                var names = existing.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s));
                var keysJson = string.Join(", ", names.Select(n => $"\"{n}\": null"));
                var newEventJson = details == null
                    ? $"\"{eventName}\": null"
                    : $"\"{eventName}\": {JsonSerializer.Serialize(details)}";
                response.Headers[headerName] = $"{{{keysJson}, {newEventJson}}}";
            }
        }
        else
        {
            // No existing header - create new JSON object
            var newEventJson = details == null
                ? $"\"{eventName}\": null"
                : $"\"{eventName}\": {JsonSerializer.Serialize(details)}";
            response.Headers[headerName] = $"{{{newEventJson}}}";
        }
    }

    /// <summary>
    /// Helper to merge raw JSON into an existing HX-Trigger* header.
    /// </summary>
    private static void MergeTriggerHeaderJson(HttpResponse response, string headerName, string json)
    {
        if (response.Headers.ContainsKey(headerName))
        {
            var existing = response.Headers[headerName].ToString();

            if (existing.TrimStart().StartsWith("{") && json.TrimStart().StartsWith("{"))
            {
                // Both are JSON objects - merge them
                var existingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existing);
                var newDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (existingDict != null && newDict != null)
                {
                    foreach (var kvp in newDict)
                    {
                        existingDict[kvp.Key] = kvp.Value;
                    }
                    response.Headers[headerName] = JsonSerializer.Serialize(existingDict);
                }
                else
                {
                    // Fallback to simple replacement if parsing fails
                    response.Headers[headerName] = json;
                }
            }
            else
            {
                // Can't merge non-JSON - just replace
                response.Headers[headerName] = json;
            }
        }
        else
        {
            response.Headers[headerName] = json;
        }
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
