# Real-time Updates (SSE)

Swap.Htmx provides built-in support for Server-Sent Events (SSE) to push updates to the client.

If you’re mixing `HX-Trigger` events and realtime broadcasts, read [Event Naming & Realtime Routing](EventNamingAndRouting.md) first.

## Packages

- Core: `Swap.Htmx`
- Realtime (SSE/WebSockets): `Swap.Htmx.Realtime`

## Single Server (In-Memory)

For simple applications running on a single server instance, you can use the in-memory backplane. This is the default behavior if no other backplane is configured, but you can also register it explicitly.

### Configuration

```csharp
// Program.cs
builder.Services.AddSwapHtmx()
    .AddSseEventBridge();

// Explicitly register the in-memory backplane (optional, as it's the default fallback)
builder.Services.AddInMemorySseBackplane();
```

### Usage

There are two common ways to push updates:

1) Preferred: emit normal Swap events (via `HX-Trigger`) and use event chains to broadcast to realtime clients.
2) Escape hatch: broadcast HTML directly via `ISseConnectionRegistry`.

```csharp
public class NotificationController : Controller
{
    private readonly ISseConnectionRegistry _sse;

    public NotificationController(ISseConnectionRegistry sse)
    {
        _sse = sse;
    }

    [HttpPost]
    public async Task<IActionResult> SendAlert()
    {
        // Broadcasts to all connected clients on this server
        // "alert" is the client-facing SSE event name (matched by `sse-swap="alert"`)
        await _sse.BroadcastAsync("alert", "<div>System Alert!</div>");
        return Ok();
    }
}
```

## Connection Behavior

- **Writes are serialized per connection.** All writes to a single SSE connection — event broadcasts and the keep-alive heartbeat — pass through a per-connection write lock, so concurrent broadcasts can't interleave or corrupt frames.
- **Keep-alive is an SSE comment.** The heartbeat is sent as a `: keepalive` comment line (not a client-visible event), so it holds the connection open without triggering any `sse-swap` handler.

## Distributed (Redis)

For applications running on multiple servers (e.g., behind a load balancer), you must use a distributed backplane like Redis to ensure events reach all clients.

[Read the Redis Backplane Documentation](RedisBackplane.md)
