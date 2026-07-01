# Public API & Compatibility

This document defines what is considered **public, supported API** in Swap.Htmx, what is **experimental**, and what is **obsolete**.

If you build against APIs listed as **Stable**, we treat changes to them as compatibility commitments under the versioning policy below.

---

## Versioning Policy (SemVer)

Swap.Htmx follows Semantic Versioning for **Stable** APIs:

- **Patch** releases (`x.y.Z`): bug fixes only; no breaking changes to Stable APIs.
- **Minor** releases (`x.Y.z`): may add Stable APIs; may add *new* Experimental APIs; may mark APIs `[Obsolete]` (with guidance).
- **Major** releases (`X.y.z`): may remove obsolete APIs and make breaking changes.

Notes:

- Anything marked **Experimental** may change in minor releases.
- Source-generator output is considered part of the public surface when it is **documented** and **named** (e.g., `SwapViews`, `SwapElements`).

---

## Packages & What They Contain

- `Swap.Htmx` (core)
  - MVC helpers: `SwapController`, extension methods for controllers
  - Results + builder: `SwapResponseBuilder`, `SwapResults`
  - Events + handlers: `EventKey`, event bus/configuration, handler abstractions
  - Tag helpers and static web assets (`swap.css`, `swap.client.js`)
  - Source generator/analyzer bundled as an analyzer (also available as `Swap.Htmx.Generators`)

- `Swap.Htmx.Realtime` (optional)
  - Realtime endpoints + middleware: SSE/WebSockets bridging
  - Helpers: `SwapRealtimeController`, `SwapRealtimeResults`, `AddSseEventBridge`, `UseSseEventBridge`

- `Swap.Htmx.Realtime.Redis` (optional)
  - Redis backplane for scaling realtime broadcasts across instances

---

## Stable API (Supported)

### App setup

Core (`Swap.Htmx`):

- Service registration:
  - `Swap.Htmx.Extensions.SwapHtmxServiceExtensions.AddSwapHtmx(...)`
- Middleware:
  - `Swap.Htmx.Extensions.SwapHtmxServiceExtensions.UseSwapHtmx()`

Realtime (`Swap.Htmx.Realtime`):

- Service registration:
  - `SwapRealtimeServiceExtensions.AddSseEventBridge(...)`
- Middleware:
  - `SwapRealtimeServiceExtensions.UseSseEventBridge()`

Redis (`Swap.Htmx.Realtime.Redis`):

- Redis backplane registration:
  - `AddSwapRedisBackplane(...)`

### Controller-first API

Core:

- `SwapController`
  - `SwapView(...)`
  - `SwapResponse()`
  - `SwapEvent(...)` / `SwapEventAsync(...)`

Extensions (when you don’t want to inherit):

- `SwapControllerExtensions`
  - `SwapView(...)`
  - `SwapResponse()`
  - `SwapEvent(...)` / `SwapEventAsync(...)`
  - `SwapRedirect(...)`

Realtime:

- `SwapRealtimeController`
  - `ServerSentEvents(...)`

### Minimal APIs

Core:

- `SwapResults.Response()`
- `SwapResults.Event(EventKey, object? payload = null)`

Realtime:

- `SwapRealtimeResults.Sse(...)`
- `SwapRealtimeResults.WebSocket(...)`

### Response builder

`SwapResponseBuilder` is stable as a concept and as the primary way to produce coordinated HTMX responses.

Stable areas include:

- Rendering:
  - `.WithView(viewName, model)`
  - `.AlsoUpdate(targetId, viewName, model, ...)` / `.AlsoUpdateIfExists(...)`
- Navigation/redirect:
  - `.WithRedirect(url)`
  - `.WithNavigation(...)`
- Events/triggers:
  - `.WithTrigger(EventKey, payload)`
  - `.WithTrigger(string eventName, payload)`
- State:
  - `.WithState(SwapState, viewName = null)`
- Result construction:
  - `.Build()` (MVC)
  - `.BuildResult()` / returning the builder directly from Minimal APIs

### Events, handlers, and configuration

Core:

- `EventKey`
- `ISwapEventConfiguration`
- `SwapEventBusOptions` (the configuration DSL)
- Handler model:
  - `ISwapEventHandler<T>`
  - `SwapEventContext<T>`
  - `[SwapHandler]` for discovery/registration

### Tag helpers & integration primitives

Core (documented tag helpers are stable):

- `<swap-nav>`
- `<swap-state>`
- `<swap-validation>`

Core model-binding attribute:

- `[FromSwapState]`

### Source generators / analyzers

Stable, documented generator entry points:

- `[SwapEventSource]` → generates `EventKey`-based nested keys
- `[SwapViewSource]` → generates view name constants
- `[SwapElementSource]` → generates element id constants

Stable generated outputs:

- `SwapViews` (auto-scan)
- `SwapElements` (auto-scan)

Analyzer diagnostics (IDs are stable):

- `SWAP001`, `SWAP002`, `SWAP004` (`SWAP003` was removed in 1.5.0)

---

## Experimental API (May Change)

The following areas may evolve without a major version bump:

- Anything under `*.Dev.*` namespaces and any dev-only endpoints/logging helpers.
- Internal telemetry tag names/Activity names (we keep the *existence* of instrumentation, but tag naming may evolve).
- Realtime routing/event-prefix conventions while they are being refined (see the dedicated conventions doc once published).

If you rely on experimental behavior, pin your version or add a small integration test around it.

---

## Obsolete / Deprecated

- `Swap.Htmx.Realtime.SseEventMiddleware` is marked `[Obsolete]` (use `RealtimeEventMiddleware` via the realtime package’s `UseSseEventBridge()` path).

---

## What Is Not Public API

These are not compatibility commitments:

- Types marked `internal`
- Anything in `*.Internal.*` namespaces
- File/namespace layout, folder names, and implementation details
- Exact HTML markup of internal framework partials/assets (unless explicitly documented as customization points)

---

## Quick “Safe Usage” Checklist

- Prefer `SwapController` + `SwapResponseBuilder` + `EventKey` constants.
- Prefer generated `SwapViews.*` / `SwapElements.*` over strings.
- Treat dev helpers, internal telemetry tags, and undocumented types as unstable.
