# SwapExpenses

A minimal expense tracker demo showcasing **Swap.Htmx.Generators**.

This project demonstrates how to use Source Generators to create type-safe event keys from simple string constants, eliminating "magic strings" from your controller and view code.

## Key Features

### 1. Source Generator (`Swap.Htmx.Generators`)
Instead of typing `"expense.added"` manually, we define it once:

```csharp
[SwapEventSource]
public partial class TrackerEvents
{
    public const string ExpenseAdded = "expense.added";
}
```

The generator automatically creates a strongly-typed hierarchy:
- `TrackerEvents.Expense.Added` (Type: `EventKey`)

### 2. Type-Safe Controllers
```csharp
return this.SwapResponse()
    .WithTrigger(TrackerEvents.Expense.Added) // Compiler checked!
    .Build();
```

### 3. Type-Safe Views
```razor
<div hx-trigger="load, @TrackerEvents.Expense.Added from:body">
    ...
</div>
```

## Project Structure
- `src/Events/TrackerEvents.cs`: The source of truth for application events.
- `src/Controllers/ExpenseController.cs`: Triggers events using generated types.
- `src/Views/Expense/Index.cshtml`: Listens for events using generated types.

