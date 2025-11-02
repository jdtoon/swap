# EventSystemDemo

A minimal ASP.NET Core MVC app demonstrating the Swap.Htmx server-side event system end-to-end.

## What it shows
- Server emits a domain event `product.created` and chains to a UI event `ui.refreshList` via `AddSwapHtmx` chain config.
- Client advertises active subscriptions with the `X-Swap-Events` request header.
- Middleware filters and merges events into the `HX-Trigger` response header (with merge if the action also sets `HX-Trigger`).

## Endpoints
- POST /Products/Create
  - Emits `product.created` -> chains to `ui.refreshList`.
- POST /Products/CreateWithTrigger
  - Sets `HX-Trigger: {"pre":"alpha"}` in the controller, then emits events; middleware merges both.

## How to run (local)
- From the repo root: dotnet run --project .\demo\EventSystemDemo\EventSystemDemo.csproj
- Use a tool like curl or Postman to send HTMX-like requests:
  - Add header: X-Swap-Events: ui.refreshList
  - POST to: http://localhost:5000/Products/Create
  - Inspect response header: HX-Trigger should include `ui.refreshList`.

Tip: The tests in `demo/EventSystemDemo.Tests` automate these flows using `Swap.Testing` helpers.
