# Observability: Logging, Tracing, and Metrics

Swap.Htmx includes comprehensive observability features to help you monitor and debug your application in both development and production.

## Logging

Swap.Htmx uses high-performance structured logging (`[LoggerMessage]`) integrated with ASP.NET Core's `ILogger`.

### Enabling Logs

Add the following to your `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Swap.Htmx": "Debug"
    }
  }
}
```
### Log Categories

All logs use the category `Swap.Htmx.*` (e.g., `Swap.Htmx.Services.SwapEventService`).

### What Gets Logged

- **Events**: Triggering and processing of Swap events.
- **Realtime**: Connection lifecycle (SSE & WebSockets), broadcasts, and rendering.
- **Results**: Execution of Swap results, toasts, and triggers.

## Distributed Tracing (OpenTelemetry)

The library uses `System.Diagnostics.ActivitySource` with the name **`Swap.Htmx`**.

### Configuration

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Swap.Htmx");
        // ... other sources
    });
```
### Available Spans (Activities)

| Activity Name | Description | Tags |
|--------------|-------------|------|
| `Swap.Htmx.ProcessEventChain` | Tracks the execution of an event chain. | `event.name`, `chain.found`, `chain.partials_count` |
| `Swap.Htmx.RealtimeBroadcast` | Tracks routing + dispatch of a realtime event (SSE + WebSockets). | `realtime.event.key`, `realtime.route.type`, `realtime.route.target`, `realtime.event.name` (also legacy `realtime.*` tags) |
| `Swap.Htmx.RealtimeRender` | Tracks rendering partials for a realtime event. | `realtime.event.name` (also legacy `realtime.event_name`) |
| `Swap.Htmx.SseDispatch` | Tracks local SSE dispatch after a backplane message is received. | `sse.event.name`, `sse.recipient.type`, `sse.recipients.count` |
| `Swap.Htmx.ResultExecute` | Tracks the execution of `SwapResult` (Minimal API). | |
| `Swap.Htmx.ActionResultExecute` | Tracks the execution of `SwapActionResult` (MVC). | |
| `Swap.Htmx.PageResultExecute` | Tracks the execution of `SwapPageResult` (Razor Pages). | |

## Metrics (OpenTelemetry)

The library uses `System.Diagnostics.Metrics.Meter` with the name **`Swap.Htmx`**.

### Configuration

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Swap.Htmx");
        // ... other meters
    });
```
### Available Metrics

| Metric Name | Type | Description |
|-------------|------|-------------|
| `swap.events.triggered` | Counter | Number of Swap events triggered. |
| `swap.events.processing_duration` | Histogram | Duration of event chain processing (ms). |
| `swap.sse.broadcasts` | Counter | Number of SSE broadcasts sent. |
| `swap.sse.connections` | UpDownCounter | Current number of active SSE connections. |

## Development Tools

Swap.Htmx includes powerful development tools to help you understand what's happening in your application.

### Enabling DevTools

DevTools are automatically enabled in the Development environment. You can also configure them manually:

```csharp
builder.Services.AddSwapHtmx(options =>
{
    options.Diagnostics.EnableClientLogging = true;     // Console logging
    options.Diagnostics.EnableDevToolsPanel = true;     // Visual panel
    options.Diagnostics.WarnOnUnhandledEvents = true;   // Server warnings
    options.Diagnostics.WarnOnMissingOobTargets = true; // OOB target validation
});
```

### Client-Side DevTools

Add the DevTools to your layout using the tag helper:

```html
@addTagHelper *, Swap.Htmx

<!-- In your _Layout.cshtml, before </body> -->
<swap-devtools />
```

This provides:

- **Event Timeline**: Visual log of all HTMX events with timing
- **Console Logging**: Detailed event information in browser console
- **State Inspection**: Track `hx-vals`, `hx-include`, and data attributes
- **OOB Tracking**: Monitor out-of-band swap operations

#### DevTools Panel

When `EnableDevToolsPanel` is true, a floating panel appears in the corner of your page showing:

- Real-time event stream
- Request/response headers
- OOB swap targets
- Event timing information

Toggle with the 🔧 button or `Ctrl+Shift+D`.

#### Console Logging

When `EnableClientLogging` is true, events are logged to the browser console:

```
[Swap] htmx:configRequest
  → path: /Products/Search
  → verb: GET
  → headers: {"HX-Request":"true"}
  → triggeringEvent: click

[Swap] htmx:beforeSwap
  → target: #product-grid
  → swapStyle: innerHTML
  
[Swap:OOB] Processing 2 out-of-band swaps
  → #search-count
  → #pagination
```

### State Debugging

For debugging state issues, add the state debug panel:

```html
<swap-state-debug target-selector="#my-form" />
```

This shows a live view of:
- All `hx-vals` values on elements
- Hidden field values
- `data-*` attributes used for state

### Server-Side Diagnostics

The `ISwapDiagnostics` service provides server-side validation:

```csharp
public class ProductsController : SwapController
{
    private readonly ISwapDiagnostics _diagnostics;
    
    public ProductsController(ISwapDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }
    
    [HttpGet]
    public IActionResult Search(string query)
    {
        // Warns in Development if "ProductSearched" has no event chain
        _diagnostics.WarnIfUnhandledEvent("ProductSearched");
        
        // ... your logic
        
        return Swap()
            .Target("#results")
            .Partial("_Results", results)
            .Trigger("ProductSearched");
    }
}
```

#### Automatic Warnings

When diagnostics are enabled, you'll see warnings for:

- **Unhandled Events**: Events triggered without configured handlers
- **Suspicious Targets**: OOB targets that may not exist (configurable patterns)

```
[Swap Warning] Event 'ProductSearched' was triggered but has no configured event chain.
Did you forget to add a configuration in AddSwapHtmx?
```

## Troubleshooting Guide

### Common Issues

**Toasts not showing?**
1. Check logs for: `Toast Notification: Success - ...` 
2. Verify HX-Trigger header contains `showToast` event.
3. Ensure toast container exists in your layout.

**Event chains not executing?**
1. Look for: `Processing Event Chain for: {name}` 
2. If you see "No chain configured", check your configuration.
3. Use `_diagnostics.WarnIfUnhandledEvent()` to catch missing chains.

**SSE not working?**
1. Check `swap.sse.connections` metric to see if clients are connecting.
2. Check logs for `SSE Connection Established`.

**State not updating correctly?**
1. Use `<swap-state-debug>` to see live state values.
2. Check event timing - use `hx-on::before-request` for state updates.
3. Enable client logging to see request parameters.

**OOB swaps not working?**
1. Enable DevTools panel to track OOB operations.
2. Verify target IDs exist in the DOM.
3. Check console for `[Swap:OOB]` messages.

### Debugging Workflow

1. **Enable DevTools** in Development:
   ```html
   <swap-devtools />
   ```

2. **Open Browser Console** (F12) to see detailed event logs.

3. **Check Event Timeline** in the DevTools panel for:
   - Which events fired
   - Request/response timing
   - OOB swap targets

4. **Use State Debug Panel** to verify state is correct:
   ```html
   <swap-state-debug target-selector="#search-form" />
   ```

5. **Check Server Logs** for warnings about unhandled events.

### Event Timing Issues

The most common issue is incorrect event timing with state updates. See [Multi-Component Coordination](./MultiComponentCoordination.md#the-event-timing-trap) for detailed guidance.

**Quick fix**: If state isn't being sent with requests, change from `after-request` to `before-request`:

```html
<!-- ❌ Wrong: State updated after request is built -->
<div hx-on::after-request="updateState()">

<!-- ✅ Correct: State updated before request is built -->
<div hx-on::before-request="updateState()">
