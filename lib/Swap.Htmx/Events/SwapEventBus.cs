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
    private IReadOnlyDictionary<string, IReadOnlyCollection<string>>? _snapshot;

    /// <summary>
    /// Configure a chain from a trigger event to one or more chained events.
    /// When the trigger event is emitted, all chained events will also be included in the HX-Trigger response.
    /// </summary>
    public SwapEventBusOptions Chain(EventKey trigger, params EventKey[] chained)
    {
        if (string.IsNullOrWhiteSpace(trigger.Name)) return this;
        if (!Chains.TryGetValue(trigger.Name, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Chains[trigger.Name] = set;
        }
        foreach (var e in chained ?? Array.Empty<EventKey>())
        {
            if (!string.IsNullOrWhiteSpace(e.Name)) set.Add(e.Name);
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

        // Validate names (skip internal storage keys like "Swap.EventChainConfigs")
        foreach (var name in Chains.Keys.Concat(Chains.Values.SelectMany(v => v)))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                diag.Errors.Add("Empty event name in chain configuration.");
                continue;
            }
            
            // Skip internal storage keys
            if (name.StartsWith("Swap.", StringComparison.Ordinal) && char.IsUpper(name[5]))
            {
                continue; // Internal key like "Swap.EventChainConfigs"
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
    public const string PendingEvents = "Swap.PendingEvents"; // List<SwapPendingEvent>
}

public interface ISwapEventBus
{
    Task EmitAsync(EventKey eventKey, object? payload = null, CancellationToken ct = default);
    void Emit(EventKey eventKey, object? payload = null);
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

    public Task EmitAsync(EventKey eventKey, object? payload = null, CancellationToken ct = default)
    {
        Emit(eventKey, payload);
        return Task.CompletedTask;
    }

    public void Emit(EventKey eventKey, object? payload = null)
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return;
        if (string.IsNullOrWhiteSpace(eventKey.Name)) return;

        var list = ctx.Items[SwapEventKeys.PendingEvents] as List<SwapPendingEvent>;
        if (list is null)
        {
            list = new List<SwapPendingEvent>();
            ctx.Items[SwapEventKeys.PendingEvents] = list;
        }

        list.Add(new SwapPendingEvent(eventKey.Name, payload));
        _logger?.LogDebug("[SwapEvents] Emitted: {Event}", eventKey.Name);
    }

    public void ClearPendingEvents()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return;
        ctx.Items.Remove(SwapEventKeys.PendingEvents);
    }

    internal IReadOnlyDictionary<string, object?> ResolveChains(HttpContext context)
    {
        var pending = context.Items[SwapEventKeys.PendingEvents] as List<SwapPendingEvent>;

        if (pending is null || pending.Count == 0)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        // Resolve chains: emit event + immediate chained events
        var resolved = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in pending)
        {
            resolved[p.Name] = p.Payload; // last write wins for duplicate events
            
            if (_options.Chains.TryGetValue(p.Name, out var chained))
            {
                foreach (var c in chained)
                {
                    if (!resolved.ContainsKey(c))
                    {
                        resolved[c] = p.Payload; // Propagate payload to chained events
                    }
                }
            }
        }

        return resolved;
    }
}
