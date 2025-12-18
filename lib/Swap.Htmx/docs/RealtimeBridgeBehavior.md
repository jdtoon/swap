# Realtime Bridge Behavior (Lifecycle & Guarantees)

This page documents the **request lifecycle** for realtime event forwarding and the behavior guarantees you can rely on when using `Swap.Htmx.Realtime`.

## What the "realtime bridge" is

When you emit events during an HTTP request (via `SwapResponse().WithTrigger(...)`, `SwapEvent(...)`, or `ISwapEventBus.Emit(...)`), Swap collects them in a per-request event bus. Near the end of the request, Swap:

1. Resolves configured **one-hop** event chains.
2. Writes the final `HX-Trigger` response header.
3. Forwards any realtime-prefixed events to the configured realtime bridge.

The realtime bridge is `IRealtimeEventBridge` (implemented by `RealtimeEventBridge`).

## Lifecycle (per request)

### 1) Events are captured during action execution

During controller/minimal API execution, Swap collects pending events in the request `HttpContext`.

### 2) `HX-Trigger` is finalized at response start

`SwapEventResponseMiddleware` builds/merges the `HX-Trigger` header in `HttpResponse.OnStarting(...)`.

This is intentionally **late** so multiple producers (toasts, event chains, manual headers) can contribute.

### 3) Realtime forwarding happens at response start

`RealtimeEventMiddleware` also uses `HttpResponse.OnStarting(...)` to forward resolved events whose keys start with:

- `sse:`
- `realtime:` (legacy)

Only those prefixed events are forwarded; normal dot-named UI/domain events remain in `HX-Trigger` only.

### Middleware ordering

Both middlewares register `OnStarting` callbacks. If you care about ordering between different `OnStarting` callbacks, control it via middleware registration order.

The recommended pipeline shape is:

```csharp
app.UseSwapHtmx();              // core Swap middleware
app.UseSseEventBridge();        // realtime forwarding (Swap.Htmx.Realtime)
```

## Guarantees

### Missing bridge does not break responses

If `IRealtimeEventBridge` is **not** registered, realtime forwarding is skipped.

### Bridge failures do not break responses

If the bridge throws while handling a realtime event (including broadcast failures), Swap:

- Logs the exception (with the event key)
- Continues the response

This makes realtime broadcasts **best-effort** by default.

### Exceptions from your HTTP handler still behave normally

This middleware does not change exception handling for your controllers/minimal APIs. If your action throws before a response is produced, normal ASP.NET Core exception handling applies.

## Tips

- Prefer dot-named events (e.g. `order.created`) for UI triggers.
- Use `sse:`/`realtime:` prefixes only for events you intend to broadcast.
- Keep payloads stable (DTOs) when events can cross service boundaries or are consumed by multiple frontends.

## See also

- [Event Naming & Realtime Routing](EventNamingAndRouting.md)
- [Server-Sent Events](ServerSentEvents.md)
- [WebSockets](WebSockets.md)
- [Event Chains](EventChains.md)
