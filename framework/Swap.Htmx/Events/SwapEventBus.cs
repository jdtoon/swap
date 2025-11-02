using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx.Events;

/// <summary>
/// Pending event captured during a request.
/// </summary>
internal sealed record SwapPendingEvent(string Name, object? Payload);

/// <summary>
/// Configuration options for the event bus, including event chains.
/// </summary>
public class SwapEventBusOptions
{
    internal Dictionary<string, HashSet<string>> Chains { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Configure a chain from a trigger event to one or more chained events.
    /// </summary>
    public SwapEventBusOptions Chain(string trigger, params string[] chained)
    {
        if (string.IsNullOrWhiteSpace(trigger)) return this;
        if (!Chains.TryGetValue(trigger, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Chains[trigger] = set;
        }
        foreach (var e in chained ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(e)) set.Add(e);
        }
        return this;
    }
}

internal static class SwapEventKeys
{
    public const string ActiveEvents = "Swap.ActiveEvents"; // HashSet<string>
    public const string PendingEvents = "Swap.PendingEvents"; // List<SwapPendingEvent>
}

public interface ISwapEventBus
{
    Task EmitAsync(string eventName, object? payload = null, CancellationToken ct = default);
    void Emit(string eventName, object? payload = null);
    void ClearPendingEvents();
}

/// <summary>
/// Captures server-to-client events during a request and defers building HX-Trigger until response.
/// Filters against active client subscriptions and resolves configured chains.
/// </summary>
public class SwapEventBus : ISwapEventBus
{
    private readonly IHttpContextAccessor _http;
    private readonly SwapEventBusOptions _options;
    private readonly ILogger<SwapEventBus>? _logger;

    public SwapEventBus(IHttpContextAccessor http, SwapEventBusOptions options, ILogger<SwapEventBus>? logger = null)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public Task EmitAsync(string eventName, object? payload = null, CancellationToken ct = default)
    {
        Emit(eventName, payload);
        return Task.CompletedTask;
    }

    public void Emit(string eventName, object? payload = null)
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return;
        if (string.IsNullOrWhiteSpace(eventName)) return;

        var list = ctx.Items[SwapEventKeys.PendingEvents] as List<SwapPendingEvent>;
        if (list is null)
        {
            list = new List<SwapPendingEvent>();
            ctx.Items[SwapEventKeys.PendingEvents] = list;
        }

        list.Add(new SwapPendingEvent(eventName, payload));
        _logger?.LogDebug("[SwapEvents] Emitted: {Event}", eventName);
    }

    public void ClearPendingEvents()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return;
        ctx.Items.Remove(SwapEventKeys.PendingEvents);
    }

    public (IReadOnlyDictionary<string, object?> resolved, int beforeFilterCount) ResolveAndFilterFor(HttpContext context)
    {
        var pending = context.Items[SwapEventKeys.PendingEvents] as List<SwapPendingEvent>;
        var active = context.Items[SwapEventKeys.ActiveEvents] as HashSet<string>;

        if (pending is null || pending.Count == 0)
        {
            return (new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase), 0);
        }

        // Resolve chains
        var resolved = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in pending)
        {
            resolved[p.Name] = p.Payload; // last write wins for payload
            if (_options.Chains.TryGetValue(p.Name, out var chained))
            {
                foreach (var c in chained)
                {
                    if (!resolved.ContainsKey(c))
                        resolved[c] = null; // chained events default to null payload
                }
            }
        }

        var beforeFilter = resolved.Count;

        // Filter by active subscriptions if present
        if (active is { Count: > 0 })
        {
            var filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in resolved)
            {
                if (active.Contains(kvp.Key)) filtered[kvp.Key] = kvp.Value;
            }
            resolved = filtered;
        }

        return (resolved, beforeFilter);
    }
}
