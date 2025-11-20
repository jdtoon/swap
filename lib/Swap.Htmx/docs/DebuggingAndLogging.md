# Observability: Logging, Tracing, and Metrics

Swap.Htmx includes comprehensive observability features to help you monitor and debug your application in both development and production.

## Logging

Swap.Htmx uses high-performance structured logging (`[LoggerMessage]`) integrated with ASP.NET Core's `ILogger`.

### Enabling Logs

Add the following to your `appsettings.json` or `appsettings.Development.json`:

`json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Swap.Htmx": "Debug"
    }
  }
}
``n
### Log Categories

All logs use the category `Swap.Htmx.*` (e.g., `Swap.Htmx.Services.SwapEventService`).

### What Gets Logged

- **Events**: Triggering and processing of Swap events.
- **SSE**: Connection lifecycle, broadcasts, and rendering.
- **Results**: Execution of Swap results, toasts, and triggers.

## Distributed Tracing (OpenTelemetry)

The library uses `System.Diagnostics.ActivitySource` with the name **`Swap.Htmx`**.

### Configuration

`csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Swap.Htmx");
        // ... other sources
    });
``n
### Available Spans (Activities)

| Activity Name | Description | Tags |
|--------------|-------------|------|
| `Swap.Htmx.EventChain` | Tracks the execution of an event chain. | `event.key`, `event.chain_length` |
| `Swap.Htmx.SseBroadcast` | Tracks the processing of an SSE broadcast. | `sse.event_key`, `sse.type`, `sse.target`, `sse.name` |
| `Swap.Htmx.SseRender` | Tracks the rendering of partials for SSE. | `sse.event_name` |
| `Swap.Htmx.ResultExecute` | Tracks the execution of `SwapResult` (Minimal API). | |
| `Swap.Htmx.ActionResultExecute` | Tracks the execution of `SwapActionResult` (MVC). | |
| `Swap.Htmx.PageResultExecute` | Tracks the execution of `SwapPageResult` (Razor Pages). | |

## Metrics (OpenTelemetry)

The library uses `System.Diagnostics.Metrics.Meter` with the name **`Swap.Htmx`**.

### Configuration

`csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Swap.Htmx");
        // ... other meters
    });
``n
### Available Metrics

| Metric Name | Type | Description |
|-------------|------|-------------|
| `swap.events.triggered` | Counter | Number of Swap events triggered. |
| `swap.events.processing_duration` | Histogram | Duration of event chain processing (ms). |
| `swap.sse.broadcasts` | Counter | Number of SSE broadcasts sent. |
| `swap.sse.connections` | UpDownCounter | Current number of active SSE connections. |

## Troubleshooting Guide

**Toasts not showing?**
1. Check logs for: `Toast Notification: Success - ...` 
2. Verify HX-Trigger header contains `showToast` event.

**Event chains not executing?**
1. Look for: `Processing Event Chain for: {name}` 
2. If you see "No chain configured", check your configuration.

**SSE not working?**
1. Check `swap.sse.connections` metric to see if clients are connecting.
2. Check logs for `SSE Connection Established`.
