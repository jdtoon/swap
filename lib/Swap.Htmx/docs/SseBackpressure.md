# SSE Backpressure (Limits & Drop Strategy)

When you broadcast SSE events, a slow client can fall behind. Swap.Htmx.Realtime provides **per-connection backpressure controls** so one browser tab can’t build an unbounded queue.

## Configure

Configure in DI when you call `AddSseEventBridge`:

```csharp
builder.Services.AddSseEventBridge(o =>
{
    o.MaxQueuedEventsPerConnection = 100;   // default
    o.MaxEventBytes = 256 * 1024;           // default (256 KB)
    o.DropStrategy = SseDropStrategy.DropOldest;
});
```

## Options

- `MaxQueuedEventsPerConnection`
  - The maximum number of pending events buffered per connection.
  - If set to `0`, events are written inline (no buffering).

- `MaxEventBytes`
  - Maximum UTF-8 payload size for a single event.
  - Oversized events are dropped.

- `DropStrategy`
  - `DropOldest`: replace the oldest queued event with the newest.
  - `DropNewest`: keep the existing queue; drop the newest event.
  - `Disconnect`: cancel the connection when it can’t keep up.

## Notes

- These limits apply to **SSE connections** (`SseConnection`) created by `ServerSentEvents(...)` / the enhanced SSE result.
- Backpressure is per-connection: one slow client won’t block broadcasts to other clients.
