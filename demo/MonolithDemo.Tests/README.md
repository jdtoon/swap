# MonolithDemo.Tests

Integration tests for the server-side Swap.Htmx event system using the CLI-generated monolith demo `MonolithDemo`.

These tests mirror the scenarios validated in `EventSystemDemo.Tests` to ensure behavior is consistent across templates.

## What’s covered

- Filtered subscriptions only deliver chained, active UI events (`ui.refreshList`)
- Unfiltered behavior includes original domain event and chained UI events
- Preexisting `HX-Trigger` header is merged (controller-set values + bus-emitted events)
- Multiple chained events are delivered together (`ui.refreshList` and `ui.showToast`)
- Duplicate emits: last payload wins for the same event key (`product.created`)
- No `X-Swap-Events` header or empty value means no filtering
- Unrelated subscriptions result in no `HX-Trigger`
- Collision override on same key: event system payload overrides pre-set header values
- ShowToast-only filter delivers only `ui.showToast`
- Extreme scenarios:
  - 100 emitted UI events and 100 subscriptions are delivered
  - 100 unrelated subscriptions result in no trigger

## How it works

- Tests use `HtmxTestFixture<MonolithDemo.AppMarker>` (WebApplicationFactory) and `HtmxTestClient` helpers from `Swap.Testing`.
- Common helpers:
  - `AsHtmxRequest()`, `WithHeader("X-Swap-Events", ...)`
  - `HtmxPostAsync(path, formData)`
  - Assertions: `AssertSuccess()`, `AssertHxTriggered(name)`, `GetHxTriggerEventNames()`, `GetHxTriggerJson()`, `GetHxTriggerRaw()`, `AssertHxTriggerFieldEquals(key, field, value)`

## Run tests

```pwsh
# Run just this project
 dotnet test .\demo\MonolithDemo.Tests\MonolithDemo.Tests.csproj -c Debug

# Or run everything
 dotnet test .\swap.sln -c Debug
```

## Notes

- Middleware logs (Information) summarize the pass via `[SwapEvents] Emitted=X Filtered=Y Active=Z`.
- Requires .NET 9 SDK.
