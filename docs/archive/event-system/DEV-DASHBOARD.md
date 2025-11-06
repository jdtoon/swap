# Swap Event Dev Dashboard

The Swap.Htmx Event System includes a development-only dashboard that visualizes your configured event chains.

What you get:
- HTML view at `/_swap/dev/events`
- JSON view at `/_swap/dev/events.json`
- Zero runtime overhead in production (only mapped in Development)

Enabling
- In apps scaffolded from the `swap-monolith` template, the dashboard is already mapped in Development.
- If wiring manually, call `app.MapSwapHtmxDevEndpoints()` after routing:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapSwapHtmxDevEndpoints();
}
```

What it shows
- A table of trigger → chained events defined in `SwapEventBusOptions` (usually via `Events/SwapEventChains.cs`).
- Counts of triggers and edges. Update your chains and refresh the page to see changes.
 - Tip: Define events as constants (e.g., `EventNames.Domain.TodoCreated`, `EventNames.Ui.TodoRefreshList`) and use those constants in your chain configuration.

Resolution modes
- Chains are directional and one-hop by default.
- Advanced options are available in code: `SwapEventBusOptions.ResolutionMode` and `MaxTransitiveDepth`.
- The dev JSON focuses on the Trigger → Chained snapshot; resolution mode is configured in code.

JSON format
```json
{
  "todo.created": ["ui.todo.refreshList", "ui.stats.refresh"],
  "bulk.completed": ["ui.todo.refreshList", "ui.toast.success"]
}
```

CLI integration
- Use `swap events from-server --url http://localhost:5000` to fetch and pretty-print the same data in your terminal.
- Or list from source: `swap events list -p .` (resolves `EventNames.*` constants)

Security note
- Endpoints only map in Development; they do not exist in Production.
