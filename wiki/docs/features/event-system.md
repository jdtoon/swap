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

## Testing

Use `Swap.Testing` helpers with WebApplicationFactory:
- `AsHtmxRequest()`, `WithHeader("X-Swap-Events", ...)`
- `HtmxPostAsync(path, formData)`
- Assertions: `AssertHxTriggered`, `GetHxTriggerEventNames`, `AssertHxTriggerFieldEquals`

See full coverage in demo test projects.

## Learn more

- Reference: docs/event-system/README.md (repo)
- CLI plan: docs/event-system/CLI-INTEGRATION.md (repo)
