using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Swap.Htmx.Events;

namespace Swap.Htmx;

public static class HtmxEvents
{
    /// <summary>
    /// Trigger a client-side HTMX event via HX-Trigger header.
    /// If payload is null, sends the event name as a simple token. Otherwise, sends a JSON map { eventName: payload }.
    /// </summary>
    public static void Trigger(HttpResponse response, string eventName, object? payload = null, string header = "HX-Trigger")
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;

        if (payload is null)
        {
            response.Headers.Append(header, eventName);
        }
        else
        {
            var json = JsonSerializer.Serialize(new Dictionary<string, object?> { { eventName, payload } });
            response.Headers.Append(header, json);
        }
    }

    /// <summary>
    /// Strongly typed overload: trigger a client-side HTMX event via HX-Trigger header using EventKey.
    /// </summary>
    public static void Trigger(HttpResponse response, EventKey eventKey, object? payload = null, string header = "HX-Trigger")
        => Trigger(response, eventKey.Name, payload, header);

    /// <summary>
    /// Trigger after swap is complete (HX-Trigger-After-Settle).
    /// </summary>
    public static void TriggerAfterSettle(HttpResponse response, string eventName, object? payload = null)
        => Trigger(response, eventName, payload, header: "HX-Trigger-After-Settle");

    /// <summary>
    /// Strongly typed overload for HX-Trigger-After-Settle using EventKey.
    /// </summary>
    public static void TriggerAfterSettle(HttpResponse response, EventKey eventKey, object? payload = null)
        => Trigger(response, eventKey.Name, payload, header: "HX-Trigger-After-Settle");

    /// <summary>
    /// Trigger after request is finished but before swap (HX-Trigger-After-Settle is usually preferred).
    /// </summary>
    public static void TriggerAfterSwap(HttpResponse response, string eventName, object? payload = null)
        => Trigger(response, eventName, payload, header: "HX-Trigger-After-Swap");

    /// <summary>
    /// Strongly typed overload for HX-Trigger-After-Swap using EventKey.
    /// </summary>
    public static void TriggerAfterSwap(HttpResponse response, EventKey eventKey, object? payload = null)
        => Trigger(response, eventKey.Name, payload, header: "HX-Trigger-After-Swap");
}
