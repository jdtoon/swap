# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)

`Swap.Htmx` is a lightweight helper library that makes it easy to build HTMX-powered ASP.NET Core MVC applications.

It focuses on:

- A base controller (`SwapController`) that understands HTMX requests and chooses between full views and partials.
- Simple extension methods for reading HX request headers and writing HX response headers.
- Middleware and an event system for building `HX-Trigger` headers in one place.
- Server‑sent events (SSE) primitives for streaming HTML updates.

## Install

- NuGet: https://www.nuget.org/packages/Swap.Htmx

```bash
dotnet add package Swap.Htmx
```

## Quick start

1. **Register services and middleware**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SwapEventBusOptions>();
builder.Services.AddScoped<ISwapEventBus, SwapEventBus>();

var app = builder.Build();

app.UseSwapHtmxVary();           // ensure Vary: HX-Request
app.UseMiddleware<SwapEventResponseMiddleware>(); // build HX-Trigger at the end of requests

app.MapControllers();

app.Run();
```

2. **Inherit from `SwapController`**

```csharp
public class TodosController : SwapController
{
    [HttpGet("/todos")]
    public IActionResult Index()
    {
        // Returns a full view for normal requests,
        // and a partial view for HTMX requests.
        return SwapView("Index", model: GetTodos());
    }
}
```

3. **Trigger client events**

```csharp
public class TodosController : SwapController
{
    [HttpPost("/todos")]
    public IActionResult Create(TodoInput input, [FromServices] ISwapEventBus events)
    {
        var todo = _service.Create(input);
        events.Emit(SwapEvents.Entity.Created("todo"));

        // HTMX request: return partial and HX-Trigger is built by middleware
        return SwapView("_Todo", todo);
    }
}
```

## Server‑sent events (SSE)

`SwapController` includes helpers for creating SSE endpoints that stream HTML fragments to the browser.

```csharp
[HttpGet("/sse/time")]
public IActionResult TimeStream()
{
    return ServerSentEvents(async (builder, ct) =>
    {
        var connection = builder.Connection;
        while (!ct.IsCancellationRequested)
        {
            var html = $"<div id=\"current-time\">{DateTime.Now:HH:mm:ss}</div>";
            await connection.SendEventAsync("time-update", html);
            await Task.Delay(1000, ct);
        }
    });
}
```

## Dev tooling

In development you can turn on simple dev endpoints to inspect event chains:

```csharp
app.MapSwapHtmxDevEndpoints(); // maps /_swap/dev/events and friends (Development only)
```

## More

For end‑to‑end examples, see the `Swap.Htmx.TestApp` project in this repository.