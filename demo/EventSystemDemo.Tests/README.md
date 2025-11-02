# EventSystemDemo.Tests

Integration tests for the server-side Swap.Htmx event system using the committed demo app `EventSystemDemo`.

These tests validate end-to-end behavior of request-scoped event collection, subscription filtering via `X-Swap-Events`, event chaining, and `HX-Trigger` header merging.

## What’s covered

- Filtered subscriptions only deliver chained, active UI events
  - Page subscribes to `ui.refreshList`; server emits `product.created` which chains to `ui.refreshList` and `ui.showToast`. Only subscribed events are delivered.
- Unfiltered behavior includes original and chained UI events
  - With no effective filter (or with a broad list including both), the original domain event and its chained UI events are delivered.
- Preexisting `HX-Trigger` header is merged
  - Controller pre-sets `HX-Trigger`; event system merges keys with last-write-wins.
- Multiple chained events are delivered together
  - Subscribed to `ui.refreshList,ui.showToast` and the request emits/chain produces both.
- Duplicate emits: last payload wins
  - Emitting the same event twice results in the last payload being present in the final header.
- No header or empty header = no filtering
  - Absence or whitespace value for `X-Swap-Events` results in no filtering.
- Unrelated subscriptions produce no `HX-Trigger`
  - If the page listens to unrelated names, no trigger is sent.
- Collision override on same key
  - If the controller sets `HX-Trigger` for `ui.refreshList` and the bus emits the same event, the event system’s payload overrides (last-write-wins).
- ShowToast-only filter
  - Subscribing only to `ui.showToast` delivers only that event.
- Extreme scenarios
  - 100 emitted UI events and 100 subscriptions all pass and are delivered.
  - 100 unrelated subscriptions result in no trigger.

## How it works

- Tests use `HtmxTestFixture<EventSystemDemo.AppMarker>` (WebApplicationFactory) and `HtmxTestClient` helpers from `Swap.Testing`.
- Common helpers:
  - `AsHtmxRequest()`, `WithHeader("X-Swap-Events", ...)`
  - `HtmxPostAsync(path, formData)`
  - Assertions: `AssertSuccess()`, `AssertHxTriggered(name)`, `GetHxTriggerEventNames()`, `GetHxTriggerJson()`, `GetHxTriggerRaw()`, `AssertHxTriggerFieldEquals(key, field, value)`

## Run tests

```pwsh
# Run just this project
 dotnet test .\demo\EventSystemDemo.Tests\EventSystemDemo.Tests.csproj -c Debug

# Or run everything
 dotnet test .\swap.sln -c Debug
```

## Notes

- Middleware logs (at Information level) include summaries like `[SwapEvents] Emitted=X Filtered=Y Active=Z` for quick diagnostics.
- Requires .NET 9 SDK.
