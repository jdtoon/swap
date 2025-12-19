# Redis Backplane for Swap.Htmx

The Redis Backplane enables horizontal scaling for Server-Sent Events (SSE) in Swap.Htmx applications. By using Redis Pub/Sub, events broadcast from one server instance are automatically propagated to all other instances, ensuring that all connected clients receive the updates regardless of which server they are connected to.

## Features

-   **Horizontal Scaling**: Run multiple instances of your application behind a load balancer.
-   **Real-time Sync**: Events are propagated instantly across the cluster.
-   **Targeted Messaging**: Supports broadcasting to all users, specific rooms, or individual users across the cluster.
-   **Automatic Reconnection**: Handles Redis connection failures gracefully.

## Installation

The Redis backplane lives in the `Swap.Htmx.Realtime.Redis` package.

```bash
dotnet add package Swap.Htmx.Realtime
dotnet add package Swap.Htmx.Realtime.Redis
```

## Configuration

### 1. Register Services

In your `Program.cs`, add the Redis backplane after adding Swap.Htmx:

```csharp
builder.Services.AddSwapHtmx()
    .AddSseEventBridge()
    .AddSwapRedisBackplane(options =>
    {
        // Connection string to your Redis instance
        options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        
        // Optional: Prefix for Redis channels to avoid collisions with other apps
        options.ChannelName = "swap-app"; 
    });
```

### 2. Configure Connection String

Add your Redis connection string to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

## Usage

Once configured, the backplane works transparently. You use the standard `ISseEventBridge` or `ISseConnectionRegistry` interfaces, and the library handles the distribution.

### Broadcasting an Event

```csharp
public class HomeController : Controller
{
  private readonly ISseConnectionRegistry _sse;

  public HomeController(ISseConnectionRegistry sse)
    {
        _sse = sse;
    }

    [HttpPost]
    public async Task<IActionResult> Broadcast()
    {
        // This event will be received by clients connected to ANY server instance
      await _sse.BroadcastAsync("my-event", "<div>Content</div>");
        return Ok();
    }
}
```

## How It Works

1.  **Publish**: When you call `BroadcastAsync`, the message is published to a Redis channel.
2.  **Subscribe**: All application instances subscribe to this Redis channel on startup.
3.  **Distribute**: When an instance receives a message from Redis, it forwards it to its local SSE connections that match the target (all, room, user, etc.).

## Docker Support

You can easily run Redis alongside your application using Docker Compose. See the `demo/SwapRedisDemo` for a complete example.

```yaml
services:
  redis:
    image: redis:alpine
    ports:
      - "6379:6379"
```
