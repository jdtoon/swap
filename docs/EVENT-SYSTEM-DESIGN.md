# Swap Event System - Intelligent, HTMX-Native Coordination

Last Updated: November 3, 2025

Status: Shipped in v0.3.0 (server + client registry + dev endpoints)

Purpose: Define the filtered, chain-based event system for the Swap framework

---

## 🎯 The Core Problem

Traditional HTMX usage broadcasts events blindly:

```csharp
// Server sends event without knowing page listeners
Response.HxTrigger("product.created");

// Every page/component might listen
<div hx-trigger="product.created from:body">...</div>
```

Problems:
- Server doesn’t know what’s on the page
- Wasted events to components that don’t exist
- No automatic event chaining (create → refresh list → update stats → toast)
- Hard to debug complex flows

---

## 💡 The Solution: Filtered Event Registry (HTMX-Native)

Core idea: the browser tells the server which events have active listeners. The server resolves chains and only sends events that are currently listened to. Zero waste.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Browser (Client)                         │
├─────────────────────────────────────────────────────────────────┤
│  1) Event Registry (HTMX-native)                                 │
│     - Scans hx-trigger for ui.* event names                      │
│     - De-dupes across DOM and after swaps                        │
│                                                                  │
│  2) HTMX Request Interceptor                                     │
│     - Adds X-Swap-Events with active UI subscriptions            │
│                                                                  │
│  3) Component Declaration                                        │
│     - Declare UI listeners directly in hx-trigger                │
│     - No custom data-* attributes required                       │
└─────────────────────────────────────────────────────────────────┘
                              ▼
                   X-Swap-Events: ui.refreshList,ui.showToast
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Server (ASP.NET Core)                       │
├─────────────────────────────────────────────────────────────────┤
│  1) Event Context Middleware                                     │
│     - Parses X-Swap-Events, stores active set in HttpContext     │
│                                                                  │
│  2) Event Bus + Chain Resolver                                   │
│     - Resolves chains: product.created → [ui.refreshList,…]      │
│     - Deduplicates and expands according to mode                 │
│                                                                  │
│  3) Filter                                                       │
│     - Keeps only events with active subscriptions                │
│                                                                  │
│  4) Response Builder                                             │
│     - Builds HX-Trigger JSON, safely merges if already set        │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Browser Receives Response                   │
├─────────────────────────────────────────────────────────────────┤
│  HTMX fires only those events that have active listeners         │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ What shipped in v0.3.0

- Server-side chain resolution with ResolutionMode:
  - OneHop (default)
  - Bidirectional
  - Transitive (with MaxTransitiveDepth)
- Client registry (hx-trigger scanner for `ui.*`) + HTMX interceptor sending `X-Swap-Events`
- Strict server-side filtering against the active set
- Middleware-based header building (`SwapEventResponseMiddleware`) via `OnStarting` with safe merging of `HX-Trigger`
- Dev endpoints (Development only):
  - `/_swap/dev/events` – HTML dashboard (table + Mermaid graph)
  - `/_swap/dev/events.json` – chains JSON
  - `/_swap/dev/events.meta.json` – current resolution mode + depth
  - `/_swap/dev/explain.json?event=...` – server-side resolution preview
- Configuration validation in Development (invalid names or cycles throw at startup)

API surface in 0.3.0:
- Configure chains and mode via `builder.Services.AddSwapHtmx(opts => { ... })`
- Register middlewares with `app.UseSwapHtmx()` and map dev endpoints in Development with `endpoints.MapSwapHtmxDevEndpoints()`
- Emit with `ISwapEventBus.Emit/EmitAsync` (prefer static names in `SwapEvents`)

---

## 🔧 Implementation Details

### 1) Client Registry (HTMX-native)

The shipped client helper (see `templates/swap-monolith/wwwroot/js/swap-events.js.template`) scans `hx-trigger` for tokens beginning with `ui.` and maintains a de-duplicated set of active UI events. It sets `X-Swap-Events` on every HTMX request and rescans after each swap.

Example component markup (no custom attributes):

```html
<!-- Product List Component listens to a UI event coming from the server -->
<div id="product-list"
     hx-get="/components/product-list"
     hx-trigger="load, ui.refreshList from:body"
     hx-swap="outerHTML"></div>
```

### 2) Server-Side: Event Context + Response Builder

Files:
- `framework/Swap.Htmx/Middleware/SwapEventContextMiddleware.cs` – Parses `X-Swap-Events` into `HttpContext.Items`
- `framework/Swap.Htmx/Middleware/SwapEventResponseMiddleware.cs` – Resolves + filters + builds `HX-Trigger` JSON late via `OnStarting` and merges safely

Configure in Program.cs:

```csharp
using Swap.Htmx;
using Swap.Htmx.Dev;
using Swap.Htmx.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwapHtmx(events =>
{
    events.Chain(SwapEvents.Entity.Created("todo"),
                 SwapEvents.UI.RefreshList,
                 SwapEvents.UI.ShowToast);
    events.ResolutionMode = ChainResolutionMode.OneHop; // or Bidirectional/Transitive
    events.MaxTransitiveDepth = 2; // only used for Transitive
});

var app = builder.Build();

app.UseSwapHtmx(); // must be before MVC endpoints

if (app.Environment.IsDevelopment())
{
    app.MapSwapHtmxDevEndpoints();
}

app.MapControllers();
app.Run();
```

Resolution modes (`framework/Swap.Htmx/Events/SwapEventBus.cs`):
- OneHop – include immediate chained events
- Bidirectional – also include reverse one-hop edges
- Transitive – breadth-first expansion up to `MaxTransitiveDepth`

### 3) Event Bus + Standard Events

Emit from controllers/services:

```csharp
public class ProductsController : SwapController
{
    private readonly ISwapEventBus _events;

    public ProductsController(ISwapEventBus events) => _events = events;

    [HttpPost]
    public async Task<IActionResult> Create(ProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id = product.Id });
        return SwapView("_ProductCard", product);
    }
}
```

Standard events (`framework/Swap.Htmx/Events/SwapEvents.cs`):

```csharp
public static class SwapEvents
{
    public static class UI
    {
        public const string RefreshList = "ui.refreshList";
        public const string OpenModal   = "ui.openModal";
        public const string CloseModal  = "ui.closeModal";
        public const string ShowToast   = "ui.showToast";
    }

    public static class Entity
    {
        public static string Created(string name) => $"{name}.created";
        public static string Updated(string name) => $"{name}.updated";
        public static string Deleted(string name) => $"{name}.deleted";
    }

    public static class Auth
    {
        public const string LoggedIn       = "auth.loggedIn";
        public const string LoggedOut      = "auth.loggedOut";
        public const string SessionExpired = "auth.sessionExpired";
    }
}
```

---

## 🧪 Dev Endpoints (Development only)

- `/_swap/dev/events` – HTML dashboard (table + Mermaid graph)
- `/_swap/dev/events.json` – chains JSON
- `/_swap/dev/events.meta.json` – current ResolutionMode/MaxTransitiveDepth
- `/_swap/dev/explain.json?event=...` – server-side resolution preview

These reflect the configured event graph under current settings and are useful for debugging server-side resolution prior to client filtering.

---

## 📊 Performance & Behavior Notes

- Header sizes remain small (hundreds of bytes typical); deduped on the client
- Filtering happens server-side against the active subscription set
- `HX-Trigger` is emitted as a JSON object and merged if already present
- Only `ui.*` events need to be declared in markup; domain events (e.g., `product.created`) are emitted by the server and typically resolve to `ui.*` events

---

## 📁 References

- Template client code: `templates/swap-monolith/wwwroot/js/swap-events.js.template`
- Middleware: `framework/Swap.Htmx/Middleware/SwapEventContextMiddleware.cs`
- Response builder: `framework/Swap.Htmx/Middleware/SwapEventResponseMiddleware.cs`
- Event bus: `framework/Swap.Htmx/Events/SwapEventBus.cs`
- Standard events: `framework/Swap.Htmx/Events/SwapEvents.cs`

This event system is the foundation for Swap’s productivity: define chains once, declare lightweight UI listeners in markup, and let the framework coordinate everything intelligently.
