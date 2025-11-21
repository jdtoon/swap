# SwapExpenses

A minimal expense tracker demo for `Swap.Htmx`.

## Features
- **Source Generators**: Uses `[SwapEventSource]` to generate type-safe event keys.
- **HTMX**: Uses `hx-trigger` to update the total and list independently.
- **Minimal UI**: Custom CSS, no heavy frameworks.

## Structure
- `src/Events/TrackerEvents.cs`: Defines the events.
- `src/Controllers/ExpenseController.cs`: Triggers events on actions.
- `src/Views/Expense/Index.cshtml`: Listens for events.
