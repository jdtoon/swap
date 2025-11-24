# SwapRedisDemo

This project demonstrates how to use the **Redis Backplane** feature of `Swap.Htmx` to scale Server-Sent Events (SSE) across multiple application instances.

## Overview

The demo consists of:
1.  **ASP.NET Core App**: A simple MVC app that allows users to "broadcast" the current time.
2.  **Redis**: Used as the message bus to propagate broadcast events between app instances.

## Prerequisites

-   .NET 9.0 SDK
-   Docker (optional, for running Redis easily)
-   Or a local Redis instance running on port 6379

## Running the Demo

### 1. Start Redis

If you have Docker installed, you can use the provided `docker-compose.yml` (if available) or just run:

```bash
docker run --name swap-redis -p 6379:6379 -d redis:alpine
```

### 2. Run Multiple Instances

Open two separate terminal windows.

**Terminal 1 (Instance A):**
```bash
cd demo/SwapRedisDemo
dotnet run --urls "http://localhost:5001"
```

**Terminal 2 (Instance B):**
```bash
cd demo/SwapRedisDemo
dotnet run --urls "http://localhost:5002"
```

### 3. Test Broadcasting

1.  Open your browser to `http://localhost:5001`.
2.  Open a second tab (or different browser) to `http://localhost:5002`.
3.  Click the **Broadcast Time via Redis** button on the first tab.
4.  Observe that **BOTH** tabs update with the new time.

This confirms that the event sent to Instance A was published to Redis, received by Instance B, and pushed to Instance B's connected clients.

## Key Code

-   **Program.cs**: Configures the Redis backplane.
    ```csharp
    builder.Services.AddSwapHtmx()
        .AddSseEventBridge()
        .AddSwapRedisBackplane(options => { ... });
    ```
-   **HomeController.cs**: Broadcasts the event.
    ```csharp
    await _registry.BroadcastAsync("sse:broadcast:redis-test", html);
    ```
-   **Index.cshtml**: Listens for the event.
    ```html
    <div hidden sse-swap="sse:broadcast:redis-test"></div>
    ```
