# Swap Event System – Reference and Usage

This is the authoritative reference for Swap's server-side event system that coordinates domain actions with UI updates via htmx headers. It covers concepts, contracts, server APIs, configuration, and testing guidance.

## Concepts

- Events: Named signals describing domain or UI changes (e.g., `product.created`, `ui.refreshList`).
- Chains: Declarative mapping from domain events to UI events (configured once).
- Subscriptions: Client advertises active interests via `X-Swap-Events` header; server filters to those.
- Emission: Controllers emit events during request processing via `ISwapEventBus`.
- Transport: Server serializes active events into `HX-Trigger` header; client handles accordingly.

## HTTP Contracts

- Request headers
  - X-Swap-Events: comma-separated event names; empty/whitespace means no filtering
  - HX-Request: optional; current behavior emits `HX-Trigger` even without this header
- Response headers
  - HX-Trigger: JSON object of eventName → payload
  - HX-Redirect: optional; used to redirect clients while preserving `HX-Trigger`

## Server APIs

- Emit an event
  ```csharp
  await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 42 });
  ```
- Configure chains
  ```csharp
  builder.Services.AddSwapHtmx(events =>
  {
      events.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);
      events.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.ShowToast);
  });
  app.UseSwapHtmx();
  ```

## Merge and Filtering semantics

- Filtering
  - No or whitespace `X-Swap-Events` → emit all collected + chained events
  - Names trimmed and deduped; matching is case-insensitive
- Merge
  - Existing `HX-Trigger` is merged best-effort with event-system output
  - On key collision, event-system payload replaces the preexisting object (last-write-wins)

## Status codes and redirects (current behavior)

- 2xx: headers emitted as expected
- 4xx/5xx: headers still emitted (e.g., 400, 500) – confirm via tests; policy may become configurable
- Redirects: prefer `HX-Redirect` header to preserve `HX-Trigger`; MVC `Redirect(...)` can be auto-followed by clients, dropping headers

## Response lifecycle

- Middleware registers `Response.OnStarting` and adds `HX-Trigger` before the response starts; a fallback path emits before completion if still not started
- Under TestServer, emitting after writing response body still included in `HX-Trigger` (see tests). Real-world servers may vary; emit before streaming when possible

## Testing guidance

- Use `HtmxTestClient` helpers in `Swap.Testing` for easy header setup and assertion
- Validate headers, not body, for event delivery
- For redirects, assert `HX-Redirect` + `HX-Trigger` together
- For robustness: test malformed preexisting `HX-Trigger`, duplicates, whitespace, and case-insensitivity

## Best practices

- Emit domain events (e.g., `product.created`) and let chains map to UI events
- Keep payloads small; headers have size limits
- Prefer `HX-Redirect` for redirect flows
- Document chains centrally; keep names lower-case and dotted

## Open questions / options

- Suppress emission for non-2xx/redirect by default? Make configurable
- Maximum chain depth and cycle handling
- Guidance for streaming/file responses across servers

## See also

- `demo/EventSystemDemo` and `demo/MonolithDemo` controllers
- Tests in `demo/*Tests` for full matrices
- Middleware: `framework/Swap.Htmx/Middleware/SwapEventResponseMiddleware.cs` and `SwapEventContextMiddleware.cs`
