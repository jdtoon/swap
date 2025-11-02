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

- Actioned_Request_With_Filtered_Subscriptions_Sends_Only_Chained_Active → Filtered-only delivery of chained events
- Actioned_Request_Without_Filter_Sends_Original_And_Chained → Unfiltered includes original + chained events
- Preexisting_HxTrigger_Is_Merged_With_SwapEvents → Merge behavior with pre-set HX-Trigger
- Multiple_Chained_Events_Are_Delivered_When_Subscribed → Both chained UI events delivered
- Duplicate_Emits_Last_Payload_Wins_For_Original_Event → Last-write-wins for duplicate domain emits
- No_Header_Treated_As_No_Filter_And_Emits_Original_And_Chained → No header means no filtering
- Empty_Header_Treated_As_No_Filter → Whitespace header means no filtering
- Unrelated_Subscriptions_Result_In_No_HxTrigger → Unrelated subs produce no trigger
- Existing_Trigger_Key_Is_Overridden_By_Event_System_On_Collision → Event-system overrides pre-set key on collision
- Filter_ShowToast_Only_Delivers_ShowToast → Single-event filter delivers only that event
- Extreme_Many_Emits_And_Subscriptions_All_Passed → 100 events delivered
- Extreme_Unrelated_Subscriptions_No_Trigger → 100 unrelated subs produce no trigger
- Duplicate_UI_Emits_Last_Payload_Wins → Last-write-wins for duplicate UI emits
- No_Events_Result_In_No_HxTrigger → No events → no header
- No_Events_With_Preexisting_Trigger_Is_Preserved → Pre-set header preserved when no events emitted
- Duplicate_Subscriptions_Are_Deduped → Duplicate header subscriptions don’t duplicate results
- Header_With_Whitespace_Is_Handled → Robustness to header whitespace
- Emit_On_BadRequest_Still_Emits_Header_Currently → 400 still includes HX-Trigger
- Emit_On_Redirect_Emits_Header_Currently → HX-Redirect + HX-Trigger present
- Subscription_Is_Case_Insensitive → Case-insensitive matching
- Emit_After_First_Write_Still_Emits_Header_Currently → Write then emit still included
- Emit_Then_Throw_InternalServerError_Still_Emits_Header_Currently → 500 still includes HX-Trigger
- Nested_Collision_Last_Write_Wins_And_Replaces_Preexisting_Object → Nested object replaced on collision
- Emits_For_Non_Htmx_Request_Currently → Header included without HX-Request

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
