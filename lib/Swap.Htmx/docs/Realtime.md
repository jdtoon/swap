# Real-time Updates (SSE)

Swap.Htmx provides built-in support for Server-Sent Events (SSE) to push updates to the client.

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
        await _sse.BroadcastAsync("alert", "<div>System Alert!</div>");
        return Ok();
    }
}
```

## Distributed (Redis)

For applications running on multiple servers (e.g., behind a load balancer), you must use a distributed backplane like Redis to ensure events reach all clients.

[Read the Redis Backplane Documentation](RedisBackplane.md)
