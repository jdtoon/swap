using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx.Events;

/// <summary>
/// Chain resolution strategies controlling how configured edges are expanded at runtime.
/// </summary>
public enum ChainResolutionMode
{
    /// <summary>
    /// Only immediate chained events are emitted (default).
    /// </summary>
    OneHop = 0,
    /// <summary>
    /// Treat edges as bidirectional for a single hop: if X→Y is configured, emitting Y also emits X.
    /// </summary>
    Bidirectional = 1,
    /// <summary>
    /// Expand directed edges transitively (breadth-first) up to MaxTransitiveDepth.
    /// </summary>
    Transitive = 2
}

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
    private IReadOnlyDictionary<string, IReadOnlyCollection<string>>? _snapshot;

    /// <summary>
    /// Controls how chains are expanded at runtime.
    /// OneHop (default): Only immediate chained events are emitted.
    /// Bidirectional: Also emit reverse one-hop edges (if X->Y configured, Y emits X too).
    /// Transitive: Expand directed edges breadth-first up to MaxTransitiveDepth.
    /// </summary>
    public ChainResolutionMode ResolutionMode { get; set; } = ChainResolutionMode.OneHop;

    /// <summary>
    /// Maximum depth for transitive expansion (>=1). Depth=1 equals OneHop semantics.
    /// Only used when ResolutionMode is Transitive. Default: 2.
    /// </summary>
    public int MaxTransitiveDepth { get; set; } = 2;

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
        _snapshot = null; // invalidate snapshot
        return this;
    }

    /// <summary>
    /// Returns an immutable snapshot of the current chains mapping.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetChainsSnapshot()
    {
        if (_snapshot is not null) return _snapshot;
        var dict = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in Chains)
        {
            dict[kv.Key] = kv.Value.ToArray();
        }
        _snapshot = dict;
        return _snapshot;
    }

    /// <summary>
    /// Validate event chains for naming conventions and cycles.
    /// Returns diagnostics with errors and warnings.
    /// </summary>
    public SwapEventDiagnostics Validate()
    {
    var diag = new SwapEventDiagnostics();
    // Allow lowercase segment start with alnum; later chars can be alnum with optional camelCase in UI segments
    var namePattern = new System.Text.RegularExpressions.Regex("^[a-z][a-z0-9]*(\\.[a-z][A-Za-z0-9]*)+$");

        // Validate names
        foreach (var name in Chains.Keys.Concat(Chains.Values.SelectMany(v => v)))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                diag.Errors.Add("Empty event name in chain configuration.");
                continue;
            }
            if (!namePattern.IsMatch(name))
            {
                diag.Errors.Add($"Invalid event name '{name}'. Use lowercase segments separated by dots, e.g., 'todo.created' or 'ui.stats.refresh'.");
            }
        }

        // Detect cycles via DFS
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool Dfs(string node)
        {
            if (!visiting.Add(node)) return true; // back-edge => cycle
            if (visited.Contains(node)) { visiting.Remove(node); return false; }

            if (Chains.TryGetValue(node, out var nexts))
            {
                foreach (var n in nexts)
                {
                    if (Dfs(n)) return true;
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }

        foreach (var trigger in Chains.Keys)
        {
            if (Dfs(trigger))
            {
                diag.Errors.Add($"Cycle detected involving '{trigger}'. Event chains must be acyclic.");
                break;
            }
        }

        return diag;
    }
}

public sealed class SwapEventDiagnostics
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
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

    internal (IReadOnlyDictionary<string, object?> resolved, int beforeFilterCount) ResolveAndFilterFor(HttpContext context)
    {
        var pending = context.Items[SwapEventKeys.PendingEvents] as List<SwapPendingEvent>;
        var active = context.Items[SwapEventKeys.ActiveEvents] as HashSet<string>;

        if (pending is null || pending.Count == 0)
        {
            return (new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase), 0);
        }

        // Resolve chains according to resolution mode
        var resolved = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        switch (_options.ResolutionMode)
        {
            case ChainResolutionMode.OneHop:
            default:
                foreach (var p in pending)
                {
                    resolved[p.Name] = p.Payload; // last write wins
                    if (_options.Chains.TryGetValue(p.Name, out var chained))
                    {
                        foreach (var c in chained)
                        {
                            if (!resolved.ContainsKey(c)) resolved[c] = null;
                        }
                    }
                }
                break;

            case ChainResolutionMode.Bidirectional:
                // One-hop in both directions: for an emitted event E,
                // include Chains[E] and any triggers T where Chains[T] contains E.
                foreach (var p in pending)
                {
                    resolved[p.Name] = p.Payload;

                    if (_options.Chains.TryGetValue(p.Name, out var chained))
                    {
                        foreach (var c in chained)
                        {
                            if (!resolved.ContainsKey(c)) resolved[c] = null;
                        }
                    }

                    // Reverse lookup (on the fly)
                    foreach (var kv in _options.Chains)
                    {
                        if (kv.Value.Contains(p.Name))
                        {
                            if (!resolved.ContainsKey(kv.Key)) resolved[kv.Key] = null;
                        }
                    }
                }
                break;

            case ChainResolutionMode.Transitive:
                int maxDepth = Math.Max(1, _options.MaxTransitiveDepth);
                foreach (var p in pending)
                {
                    resolved[p.Name] = p.Payload;

                    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { p.Name };
                    var queue = new Queue<(string node, int depth)>();
                    queue.Enqueue((p.Name, 0));

                    while (queue.Count > 0)
                    {
                        var (node, depth) = queue.Dequeue();
                        if (depth >= maxDepth) continue;

                        if (_options.Chains.TryGetValue(node, out var nexts))
                        {
                            foreach (var n in nexts)
                            {
                                if (!visited.Add(n)) continue;
                                if (!resolved.ContainsKey(n)) resolved[n] = null;
                                queue.Enqueue((n, depth + 1));
                            }
                        }
                    }
                }
                break;
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
