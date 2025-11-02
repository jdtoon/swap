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
- Non-2xx and redirect behaviors (current observations)
  - 400 BadRequest after emit still includes HX-Trigger
  - HX-Redirect header approach preserves HX-Trigger for assertions and client redirect
- Robustness and semantics
  - Case-insensitive subscriptions are honored (e.g., `UI.RefreshList`)
  - Emitting after writing the response body still results in HX-Trigger under TestServer
  - Emitting then throwing (500) still includes HX-Trigger
  - Nested collision on same key replaces the preexisting object (last-write-wins)
  - Non-htmx requests (no `HX-Request`) currently still receive HX-Trigger

## Test matrix (method → scenario)

- Filtered_Subscriptions_Only_Chained_Active → Filtered-only delivery of chained events
- Merge_Preexisting_HxTrigger_With_SwapEvents → Merge behavior with pre-set HX-Trigger
- Extreme_Subscriptions_And_Emits → 100 events delivered
- Duplicate_UI_Emits_Last_Payload_Wins → Last-write-wins for duplicate UI emits
- No_Events_Result_In_No_HxTrigger → No events → no header
- No_Events_With_Preexisting_Trigger_Is_Preserved → Pre-set header preserved when no events emitted
- Duplicate_Subscriptions_Are_Deduped → Duplicate header subscriptions don’t duplicate results
- Header_With_Whitespace_Is_Handled → Robustness to header whitespace
- Concurrency_Isolation_Across_Parallel_Requests → Per-request isolation under parallel load
- Emit_On_BadRequest_Still_Emits_Header_Currently → 400 still includes HX-Trigger
- Emit_On_Redirect_Emits_Header_Currently → HX-Redirect + HX-Trigger present
- Subscription_Is_Case_Insensitive → Case-insensitive matching
- Emit_After_First_Write_Still_Emits_Header_Currently → Write then emit still included
- Emit_Then_Throw_InternalServerError_Still_Emits_Header_Currently → 500 still includes HX-Trigger
- Nested_Collision_Last_Write_Wins_And_Replaces_Preexisting_Object → Nested object replaced on collision
- Emits_For_Non_Htmx_Request_Currently → Header included without HX-Request

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
