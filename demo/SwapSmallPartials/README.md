# SwapSmallPartials Demo

A stress test demo showing how Swap.Htmx handles pages with **25+ small partials** that update in coordination when events fire.

## Overview

This demo displays a single page with **25 small partials**:
- 10 Counters
- 5 Status Toggles  
- 5 Progress Bars
- 5 Aggregate Values (Total, Average, Max, Min, Active Statuses)

When you click on any element, **multiple partials update automatically** via Out-of-Band (OOB) swaps through distributed event handlers.

## Quick Start

```bash
cd demo/SwapSmallPartials/src
libman restore
dotnet run
```

Visit: **http://localhost:5050/partials**

## Key Concepts Demonstrated

### 1. Distributed Event Handlers
Each handler updates ONE piece of the UI:

```csharp
[SwapHandler]
public class TotalHandler : ISwapEventHandler<CounterIncrementedEvent>
{
    public Task HandleAsync(CounterIncrementedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-total", "_Total", _state);
        return Task.CompletedTask;
    }
}
```

### 2. Event Chains
When a counter increments:
- The counter partial updates
- The Total partial updates (via TotalHandler)
- The Average partial updates (via AverageHandler)
- The Max partial updates (via MaxHandler)
- The Min partial updates (via MinHandler)

**ONE click → 5 partials updated → ONE HTTP response**

### 3. Bulk Updates
The "Reset All" button triggers an event that updates ALL 25 partials in a single response!

## Architecture

```
Modules/Partials/
├── Controllers/
│   └── PartialsController.cs     # Controller with actions
├── Events/
│   ├── PartialsEvents.cs         # [SwapEventSource] event keys
│   ├── PartialsEventConfig.cs    # Event chain configuration
│   └── Handlers/
│       └── PartialsHandlers.cs   # Distributed handlers
├── PartialsModule.cs             # Service registration
└── Views/
    ├── Index.cshtml              # Main page with all partials
    ├── _Counter.cshtml           # Counter partial (x10)
    ├── _Status.cshtml            # Status partial (x5)
    ├── _Progress.cshtml          # Progress partial (x5)
    ├── _Total.cshtml             # Aggregate: sum
    ├── _Average.cshtml           # Aggregate: mean
    ├── _Max.cshtml               # Aggregate: maximum
    ├── _Min.cshtml               # Aggregate: minimum
    └── _ActiveStatuses.cshtml    # Aggregate: count of active
```

## What to Test

1. **Single Click** - Click any "+1" button on a counter
   - Watch the counter update
   - Watch Total, Average, Max, Min all update
   - **5 partials update from 1 click**

2. **Toggle Status** - Click any "Toggle" button
   - Watch the status flip
   - Watch "Active Statuses" count change
   - **2 partials update from 1 click**

3. **Bulk Actions** - Click the bulk action buttons at the top
   - "Increment All Counters" - Updates 14 partials
   - "Toggle All Statuses" - Updates 6 partials
   - "Reset All" - Updates **25 partials in one response!**

4. **Open Network Tab** - Watch the HTTP responses
   - Every response contains multiple `hx-swap-oob` elements
   - The system scales well with many concurrent OOB swaps

## Why This Matters

This demo proves that Swap.Htmx can handle complex UIs with:
- Many small, independent partials
- Cross-cutting concerns (aggregates depend on individual values)
- Coordinated updates via events
- Single HTTP response containing all updates

The key insight: **Controllers fire events, Handlers update UI**. Adding a new widget is just adding a new handler - no controller changes needed.

## Learn More

- [Swap.Htmx Documentation](https://github.com/jdtoon/swap)
- [HTMX Documentation](https://htmx.org)
