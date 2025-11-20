using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Extensions for building simple polling endpoints alongside SSE.
/// These helpers are optional and are not wired to any client-side
/// fallback script; use them when you explicitly want polling.
/// </summary>
public static class SseFallbackExtensions
{
    /// <summary>
    /// Creates a polling endpoint that can serve HTML responses keyed off an
    /// optional <c>Last-Event-ID</c> value. This is protocol-agnostic and can
    /// be used either as a manual SSE fallback or as a standalone polling
    /// endpoint.
    /// </summary>
    /// <param name="controller">The SwapController instance.</param>
    /// <param name="getContentFunc">Function to get the current content for polling.</param>
    /// <param name="lastEventId">Optional last event ID for differential updates.</param>
    /// <returns>IActionResult with either full content or partial updates.</returns>
    public static async Task<Microsoft.AspNetCore.Mvc.IActionResult> PollingFallback(
        this Swap.Htmx.SwapController controller,
        Func<string?, Task<string>> getContentFunc,
        string? lastEventId = null)
    {
        // Check if this is a polling request
        var isPolling = controller.Request.Headers.ContainsKey("Last-Event-ID") ||
                       controller.Request.Query.ContainsKey("poll") ||
                       controller.Request.Path.Value?.EndsWith("/poll") == true;

        // Get the last event ID from headers or query
        lastEventId ??= controller.Request.Headers["Last-Event-ID"].FirstOrDefault() ??
                       controller.Request.Query["lastEventId"].FirstOrDefault();

        try
        {
            var content = await getContentFunc(lastEventId);

            if (string.IsNullOrEmpty(content))
            {
                // No updates available
                return controller.NoContent();
            }

            // For polling requests, return plain HTML
            if (isPolling)
            {
                controller.Response.Headers["X-Polling-Fallback"] = "true";
                controller.Response.Headers["X-Event-ID"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                return controller.Content(content, "text/html");
            }

            // For non-polling requests, return the current HTML snapshot
            return controller.Content(content, "text/html");
        }
        catch (Exception)
        {
            // Return empty response on error to avoid breaking the polling loop
            return controller.NoContent();
        }
    }

    /// <summary>
    /// Creates a polling endpoint with built-in caching and differential updates.
    /// </summary>
    /// <param name="controller">The SwapController instance.</param>
    /// <param name="cacheKey">Cache key for storing the last content.</param>
    /// <param name="getContentFunc">Function to get the current content.</param>
    /// <param name="cacheDuration">How long to cache content (default: 30 seconds).</param>
    /// <returns>IActionResult with cached or updated content.</returns>
    public static async Task<Microsoft.AspNetCore.Mvc.IActionResult> CachedPollingFallback(
        this Swap.Htmx.SwapController controller,
        string cacheKey,
        Func<Task<string>> getContentFunc,
        TimeSpan? cacheDuration = null)
    {
        var cache = controller.HttpContext.RequestServices.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        if (cache == null)
        {
            // Fall back to simple polling without caching
            return await controller.PollingFallback(_ => getContentFunc());
        }

        cacheDuration ??= TimeSpan.FromSeconds(30);
        var lastEventId = controller.Request.Headers["Last-Event-ID"].FirstOrDefault();
        var cacheEntry = $"{cacheKey}:content";
        var timestampEntry = $"{cacheKey}:timestamp";

        // Check if we have cached content
        if (cache.TryGetValue(cacheEntry, out var cachedContentObj) && cachedContentObj is string cachedContent &&
            cache.TryGetValue(timestampEntry, out var cachedTimestampObj) && cachedTimestampObj is long cachedTimestamp)
        {
            // If client has the latest content, return no content
            if (lastEventId != null && long.TryParse(lastEventId, out var clientTimestamp) &&
                clientTimestamp >= cachedTimestamp)
            {
                return controller.NoContent();
            }

            // Return cached content with timestamp
            controller.Response.Headers["X-Event-ID"] = cachedTimestamp.ToString();
            return controller.Content(cachedContent, "text/html");
        }

        // Generate new content
        var newContent = await getContentFunc();
        var newTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Cache the content
        cache.Set(cacheEntry, newContent, cacheDuration.Value);
        cache.Set(timestampEntry, newTimestamp, cacheDuration.Value);

        controller.Response.Headers["X-Event-ID"] = newTimestamp.ToString();
        return controller.Content(newContent ?? "", "text/html");
    }

    /// <summary>
    /// Creates a polling endpoint that returns JSON data instead of HTML.
    /// Useful for JavaScript-based updates or custom front-end clients.
    /// </summary>
    /// <param name="controller">The SwapController instance.</param>
    /// <param name="getDataFunc">Function to get the current data.</param>
    /// <param name="lastEventId">Optional last event ID.</param>
    /// <returns>IActionResult with JSON data or no content.</returns>
    public static async Task<Microsoft.AspNetCore.Mvc.IActionResult> JsonPollingFallback<T>(
        this Swap.Htmx.SwapController controller,
        Func<string?, Task<T?>> getDataFunc,
        string? lastEventId = null)
    {
        lastEventId ??= controller.Request.Headers["Last-Event-ID"].FirstOrDefault() ??
                       controller.Request.Query["lastEventId"].FirstOrDefault();

        try
        {
            var data = await getDataFunc(lastEventId);

            if (data == null)
            {
                return controller.NoContent();
            }

            controller.Response.Headers["X-Polling-Fallback"] = "true";
            controller.Response.Headers["X-Event-ID"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            return controller.Json(data);
        }
        catch (Exception)
        {
            return controller.NoContent();
        }
    }
}

/// <summary>
/// Configuration for polling behaviour when used alongside SSE. This type
/// does not control any automatic client-side fallback; it simply provides
/// server-side defaults for polling intervals and diagnostics.
/// </summary>
public class SseFallbackOptions
{
    /// <summary>
    /// Default polling interval in milliseconds.
    /// </summary>
    public int DefaultPollInterval { get; set; } = 5000;

    /// <summary>
    /// Maximum number of SSE retries before falling back to polling (if you
    /// implement such logic on the client).
    /// </summary>
    public int MaxSseRetries { get; set; } = 3;

    /// <summary>
    /// Number of SSE errors before switching to polling on the client.
    /// </summary>
    public int FallbackAfterErrors { get; set; } = 2;

    /// <summary>
    /// Whether polling endpoints should be considered enabled by default in
    /// your own application logic.
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Whether to include debug information in responses.
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Custom headers to include in polling responses.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Service for managing polling configuration when used together with SSE.
/// This is an optional abstraction that you can use to centralise how your
/// app decides between SSE and polling.
/// </summary>
public interface ISseFallbackService
{
    /// <summary>
    /// Gets the current fallback options.
    /// </summary>
    SseFallbackOptions Options { get; }

    /// <summary>
    /// Determines if a request should use polling fallback.
    /// </summary>
    bool ShouldUsePolling(Microsoft.AspNetCore.Http.HttpContext context);

    /// <summary>
    /// Gets the appropriate polling interval for a request.
    /// </summary>
    int GetPollInterval(Microsoft.AspNetCore.Http.HttpContext context);
}

/// <summary>
/// Default implementation of <see cref="ISseFallbackService"/>. It uses
/// simple header and query-string conventions to decide when to poll and
/// how frequently.
/// </summary>
internal class SseFallbackService : ISseFallbackService
{
    private readonly SseFallbackOptions _options;

    public SseFallbackService(SseFallbackOptions options)
    {
        _options = options ?? new SseFallbackOptions();
    }

    public SseFallbackOptions Options => _options;

    public bool ShouldUsePolling(Microsoft.AspNetCore.Http.HttpContext context)
    {
        // Check for polling indicators
        return context.Request.Headers.ContainsKey("Last-Event-ID") ||
               context.Request.Query.ContainsKey("poll") ||
               context.Request.Path.Value?.EndsWith("/poll") == true ||
               context.Request.Headers["User-Agent"].ToString().Contains("curl") || // CLI tools
               !AcceptsSse(context);
    }

    public int GetPollInterval(Microsoft.AspNetCore.Http.HttpContext context)
    {
        // Check for custom interval in query parameters
        if (context.Request.Query.TryGetValue("interval", out var intervalStr) &&
            int.TryParse(intervalStr, out var customInterval) &&
            customInterval > 0 && customInterval <= 60000) // Max 1 minute
        {
            return customInterval;
        }

        return _options.DefaultPollInterval;
    }

    private static bool AcceptsSse(Microsoft.AspNetCore.Http.HttpContext context)
    {
        var accept = context.Request.Headers["Accept"].ToString();
        return accept.Contains("text/event-stream") || accept.Contains("text/html");
    }
}