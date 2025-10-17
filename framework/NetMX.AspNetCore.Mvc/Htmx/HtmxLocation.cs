using System.Text.Json;

namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Represents a location for client-side redirects with additional context.
/// Used with the HX-Location header to provide rich redirect information.
/// </summary>
public class HtmxLocation
{
    /// <summary>
    /// The URL to redirect to.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The source element of the request.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// An event to trigger.
    /// </summary>
    public string? Event { get; set; }

    /// <summary>
    /// The target element to swap.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// How the response will be swapped.
    /// </summary>
    public string? Swap { get; set; }

    /// <summary>
    /// Values to submit with the request.
    /// </summary>
    public object? Values { get; set; }

    /// <summary>
    /// Headers to submit with the request.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Creates a new HtmxLocation.
    /// </summary>
    /// <param name="path">The URL to redirect to.</param>
    public HtmxLocation(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Sets the source element of the request.
    /// </summary>
    public HtmxLocation WithSource(string source)
    {
        Source = source;
        return this;
    }

    /// <summary>
    /// Sets an event to trigger.
    /// </summary>
    public HtmxLocation WithEvent(string eventName)
    {
        Event = eventName;
        return this;
    }

    /// <summary>
    /// Sets the target element to swap.
    /// </summary>
    public HtmxLocation WithTarget(string target)
    {
        Target = target;
        return this;
    }

    /// <summary>
    /// Sets how the response will be swapped.
    /// </summary>
    public HtmxLocation WithSwap(string swap)
    {
        Swap = swap;
        return this;
    }

    /// <summary>
    /// Sets values to submit with the request.
    /// </summary>
    public HtmxLocation WithValues(object values)
    {
        Values = values;
        return this;
    }

    /// <summary>
    /// Sets headers to submit with the request.
    /// </summary>
    public HtmxLocation WithHeaders(Dictionary<string, string> headers)
    {
        Headers = headers;
        return this;
    }

    /// <summary>
    /// Adds a header to submit with the request.
    /// </summary>
    public HtmxLocation WithHeader(string name, string value)
    {
        Headers ??= new Dictionary<string, string>();
        Headers[name] = value;
        return this;
    }

    /// <summary>
    /// Converts the location to a JSON string for the HX-Location header.
    /// </summary>
    public string ToJson()
    {
        var obj = new Dictionary<string, object>
        {
            ["path"] = Path
        };

        if (!string.IsNullOrEmpty(Source)) obj["source"] = Source;
        if (!string.IsNullOrEmpty(Event)) obj["event"] = Event;
        if (!string.IsNullOrEmpty(Target)) obj["target"] = Target;
        if (!string.IsNullOrEmpty(Swap)) obj["swap"] = Swap;
        if (Values != null) obj["values"] = Values;
        if (Headers != null && Headers.Count > 0) obj["headers"] = Headers;

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
