# SwapDebtors

A comprehensive Swap.Htmx demo application demonstrating:
- **SwapState** for server-driven state management with filtering and pagination
- **SwapViews / SwapElements** auto-generated constants
- **SwapEventSource** for declarative event handling
- **ISwapEventConfiguration** for event-driven UI updates
- **Multi-currency** support with external API integration
- **Real-time SSE** activity feed

## Features Demonstrated

### 1. SwapState (Server-Driven State)

The Debtors and Debts pages use `SwapState` for:
- **Filtering** - Search, category, status filters
- **Sorting** - Multiple sort options with direction toggle
- **Pagination** - Page-based navigation with state persistence

```csharp
// State class
public class DebtorFilterState : SwapState
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
}

// Controller action using [FromSwapState]
[HttpGet]
public async Task<IActionResult> Filter([FromSwapState] DebtorFilterState state)
{
    var (debtors, totalCount) = await FilterDebtorsAsync(state);
    return PartialView("_FilterContent", new DebtorListViewModel { State = state, ... });
}
```

In the view, use `<swap-state>` tag helper:
```html
<swap-state state="Model.State" />

<!-- Use hx-include to send state with requests -->
<button hx-get="/Debtors/Filter?Page=2"
        hx-target="#filter-content"
        hx-include="#@state.ContainerId">
    Page 2
</button>
```

### 2. Auto-Generated Constants

Source generators automatically create type-safe constants from your views:

```csharp
// Generated from Views/Dashboard/_Stats.cshtml
return SwapView(SwapViews.Dashboard._Stats, model);

// Generated from id="stats" in views
.RefreshPartial(SwapElements.Stats, SwapViews.Dashboard._Stats, ctx => ...)
```

To see generated files, check: `obj/Generated/Swap.Htmx.Generators/`

Enable output in `.csproj`:
```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>obj\Generated</CompilerGeneratedFilesOutputPath>
```

### 3. Event-Driven UI Updates

Events declared with `[SwapEventSource]`:
```csharp
[SwapEventSource("Debtor")]
public static partial class DebtorEvents;
// Generates: DebtorEvents.Debtor.Created, .Updated, .Deleted
```

Configured with `ISwapEventConfiguration`:
```csharp
public class DebtorEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(DebtorEvents.Debtor.Created)
            .RefreshPartial(SwapElements.DebtorList, SwapViews.Dashboard._DebtorList, ctx => ...)
            .RefreshPartial(SwapElements.Stats, SwapViews.Dashboard._Stats, ctx => ...)
            .Toast("Debtor created", ToastType.Success);
    }
}
```

Triggered from controller:
```csharp
return (await SwapEventAsync(
    DebtorEvents.Debtor.Created,
    new DebtorCreatedEvent { Id = debtor.Id, Name = debtor.Name }))
    .WithClientAction("closeModal")
    .Build();
```

### 4. Real-time Activity Feed (SSE)

The dashboard includes a real-time activity feed using Server-Sent Events:

```csharp
// Program.cs
app.MapSwapSse("/api/activity-stream");

// Handler with [SwapHandler]
[SwapHandler]
public class DebtorActivityHandler
{
    public void Handle(DebtorCreatedEvent @event, SwapHandlerContext context)
    {
        context.SendSse("/api/activity-stream", ...);
    }
}
```

In view:
```html
<div hx-ext="sse" sse-connect="/api/activity-stream">
    <div id="activity-feed" sse-swap="activity">
```

## Running

```bash
cd demo/SwapDebtors/src/SwapDebtors
dotnet run
```

Then open http://localhost:5298

## Tech Stack

- .NET 10 Preview
- Swap.Htmx library
- SQLite with EF Core
- HTMX 2.0.8 (via libman)
- PicoCSS classless framework
- exchangerate-api.com for currency rates (with fallback)
