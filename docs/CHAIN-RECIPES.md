# Chain Recipes and Naming

Keep it simple. Use lowercase dot-separated names: `domain.action` for domain events, and `ui.area.action` for UI updates. In your app, define them as constants (e.g., `Events/EventNames.cs`) and reference the constants everywhere for traceability.

Naming rules (enforced with guardrails)
- Must match: `^[a-z][a-z0-9]*(\.[a-z][A-Za-z0-9]*)+$`
- Examples: `EventNames.Domain.TodoCreated`, `EventNames.Domain.TodoToggled`, `EventNames.Ui.TodoRefreshList`, `EventNames.Ui.StatsRefresh`
- Avoid capitals, spaces, or underscores.

Core patterns
- Create → refresh owner list + cross-panels
  - `EventNames.Domain.TodoCreated -> EventNames.Ui.TodoRefreshList, EventNames.Ui.StatsRefresh, EventNames.Ui.ToastSuccess`
- Delete → minimal blast radius
  - `EventNames.Domain.TodoDeleted -> EventNames.Ui.StatsRefresh, EventNames.Ui.ToastSuccess` (no list refresh; local delete via `hx-swap="delete"`)
- Toggle → counters only
  - `EventNames.Domain.TodoToggled -> EventNames.Ui.StatsRefresh`
- Bulk complete → list + counters + toast
  - `EventNames.Domain.BulkCompleted -> EventNames.Ui.TodoRefreshList, EventNames.Ui.StatsRefresh, EventNames.Ui.ToastSuccess`

Reusable components (instance-scoped)
- Give each instance a unique UI namespace:
  - `EventNames.Domain.ComponentAUpdated -> EventNames.Ui.ComponentARefresh, EventNames.Ui.ToastSuccess`
  - `EventNames.Domain.ComponentBUpdated -> EventNames.Ui.ComponentBRefresh, EventNames.Ui.ToastSuccess`

How to wire
1) Define events and chains centrally:
```csharp
// Events/EventNames.cs
public static class EventNames
{
    public static class Domain
    {
        public const string TodoCreated = "todo.created";
        public const string TodoDeleted = "todo.deleted";
        public const string TodoToggled = "todo.toggled";
        public const string BulkCompleted = "bulk.completed";
    }
    public static class Ui
    {
        public const string TodoRefreshList = "ui.todo.refreshList";
        public const string StatsRefresh = "ui.stats.refresh";
        public const string ToastSuccess = "ui.toast.success";
    }
}

// Events/SwapEventChains.cs
events
  .Chain(EventNames.Domain.TodoCreated, EventNames.Ui.TodoRefreshList, EventNames.Ui.StatsRefresh, EventNames.Ui.ToastSuccess)
  .Chain(EventNames.Domain.TodoDeleted, EventNames.Ui.StatsRefresh, EventNames.Ui.ToastSuccess)
  .Chain(EventNames.Domain.TodoToggled, EventNames.Ui.StatsRefresh);
```
2) Make containers self-owned and listen via `hx-trigger="load, ui.* from:body"`.
3) Emit domain events from controllers/services via `ISwapEventBus.Emit(...)`.

Guardrails
- Invalid names or cycles cause a startup error in Development. Check your console output and fix the chain.

Testing ergonomics
- Use `Swap.Testing` helpers to assert HX-Trigger contents and chain behavior:
  - `AssertHxTriggered(EventNames.Ui.TodoRefreshList)`
  - `GetHxTriggerEventNames()`

Performance notes
- Chain resolution is per-request, in-memory, O(E) where E is number of edges referenced by emitted events.
- Active subscription filtering uses a HashSet; only subscribed events are sent to the client.

Tooling
- Dev dashboard: `/_swap/dev/events` (HTML) and `/_swap/dev/events.json` (JSON)
- CLI: `swap events list` (source scan) or `swap events from-server --url http://localhost:5000`

Resolution modes (advanced)
- Default is OneHop (directional, immediate children only).
- `Bidirectional`: treat edges as mirrored for a single hop (X→Y implies emitting Y also includes X).
- `Transitive`: breadth-first expansion along edges up to `MaxTransitiveDepth`.

Example configuration:
```csharp
builder.Services.AddSwapHtmx(opts =>
{
  opts.Chain(EventNames.Domain.TodoCreated, EventNames.Ui.TodoRefreshList, EventNames.Ui.StatsRefresh);
  // defaults to OneHop
  opts.ResolutionMode = ChainResolutionMode.OneHop;

  // Alternatives:
  // opts.ResolutionMode = ChainResolutionMode.Bidirectional;
  // opts.ResolutionMode = ChainResolutionMode.Transitive;
  // opts.MaxTransitiveDepth = 2; // when Transitive
});
```
