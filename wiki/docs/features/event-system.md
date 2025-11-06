---
id: event-system
title: Event System
slug: /features/event-system
---

The Event System is the foundation of Swap. It coordinates server-side domain actions and client-side UI updates using htmx headers.

## Why it matters

- Emit domain events in controllers; let chains map to UI events
- Only events with active listeners are sent (via X-Swap-Events)
- Debuggable: clear logs and predictable behavior

## Core concepts

- Events: `product.created`, `ui.refreshList`, `ui.showToast`
- Chains: configure domain → UI events once in Program.cs
- Subscriptions: client advertises `X-Swap-Events: name1,name2,...`
- Transport: server emits `HX-Trigger` JSON; redirect via `HX-Redirect`

## Server quick start

```csharp
// Program.cs
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);
    events.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.ShowToast);
});
app.UseSwapHtmx();

// Controller
await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 42 });
```

## Client behavior

- Active components send `X-Swap-Events`
- htmx receives `HX-Trigger` and dispatches events accordingly
- For redirects, prefer `HX-Redirect` (keeps headers intact)

## Semantics

- Filtering: empty/whitespace = no filter; names are deduped and case-insensitive
- Merge: existing `HX-Trigger` merged; last-write-wins on key collision
- Status codes (current behavior): 2xx, 4xx, 5xx still include `HX-Trigger`; may become configurable
- Lifecycle: headers are set in `Response.OnStarting`; under TestServer, write-then-emit still included

### Chain resolution modes

Control expansion with a single enum (default: OneHop):

```csharp
builder.Services.AddSwapHtmx(opts =>
{
    opts.Chain("todo.created", "ui.todo.refreshList", "ui.stats.refresh");
    opts.ResolutionMode = Swap.Htmx.Events.ChainResolutionMode.OneHop; // default
    // opts.ResolutionMode = ChainResolutionMode.Bidirectional; // reverse one-hop
    // opts.ResolutionMode = ChainResolutionMode.Transitive;    // BFS up to MaxTransitiveDepth
    // opts.MaxTransitiveDepth = 2;
});
```

Semantics:
- OneHop: A emits its immediate children only.
- Bidirectional: A→B means emitting B also includes A (one hop each way).
- Transitive: A→B→C expands along edges up to the configured depth.

## Testing

Use `Swap.Testing` helpers with WebApplicationFactory:
- `AsHtmxRequest()`, `WithHeader("X-Swap-Events", ...)`
- `HtmxPostAsync(path, formData)`
- Assertions: `AssertHxTriggered`, `GetHxTriggerEventNames`, `AssertHxTriggerFieldEquals`

See full coverage in demo test projects.

## Server Events (RabbitMQ)

For modular monoliths or distributed deployments, Swap supports RabbitMQ-based server events that propagate across processes.

### Configuration

**appsettings.json:**
```json
{
  "Swap": {
    "ServerEvents": {
      "Enabled": true,
      "ConnectionString": "amqp://guest:guest@localhost:5672",
      "ExchangeName": "swap.events",
      "QueuePrefix": "MyApp"
    }
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddSwapServerEventChainsFromConfiguration(
    builder.Configuration,
    "Swap:ServerEvents"
);
```

### Server Event Chains

Modules can listen for events from RabbitMQ:

```csharp
public class OrdersModule : IModule
{
    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // Listen for server events
        registrar.Register("order.created", async (OrderCreated evt) =>
        {
            await UpdateInventoryAsync(evt.ProductId, evt.Quantity);
        });
    }
}
```

**Emitting Server Events:**
```csharp
// Events are automatically published to RabbitMQ when server events are enabled
await _events.EmitAsync(SwapEvents.Custom("order.created"), order);
```

## Learn more

- **[Comprehensive Event Guide](https://github.com/jdtoon/swap/blob/main/docs/EVENTS.md)** — Full documentation on chain resolution, RabbitMQ integration, testing, and real-world examples
- Reference: docs/event-system/README.md (repo)
- CLI plan: docs/event-system/CLI-INTEGRATION.md (repo)
- Dev Dashboard: `/_swap/dev/events` and `/_swap/dev/events.json`
- CLI: `swap events list` and `swap events from-server --url http://localhost:5000`

