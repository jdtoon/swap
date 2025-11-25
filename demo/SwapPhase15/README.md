# SwapPhase15 Demo

This demo showcases the new features introduced in Phase 1.5 of Swap.Htmx, focusing on UI orchestration with event chains.

## Features Demonstrated

### 1. Distributed UI Handlers (`ISwapEventHandler<T>`)
- Handlers are automatically discovered via assembly scanning.
- Decoupled event processing without central configuration.
- Priority-based execution ordering.
- Example: `UserClickedHandler` and `CounterUpdatedHandler`.

### 2. Auto-Validation Filter (`[SwapForm]`)
- Automatic model validation for HTMX requests.
- Returns validation errors without boilerplate code.
- Reduces repetitive `if (!ModelState.IsValid)` checks.
- Demo: Submit empty form to see validation error.

### 3. Client Actions Protocol
- Declarative client-side actions like focus, reset, scroll.
- Triggered from server responses.
- Extensible for custom actions.

### 4. Event Chains for UI Orchestration
- Chains allow one event to trigger multiple UI updates.
- Example: Incrementing counter triggers `counter.updated`, which chains to `stats.updated`, updating both counter and stats sections.
- Demonstrates how updating one component can orchestrate updates across the entire UI.

### 5. SwapEventSource Integration
- Type-safe event keys generated from constants.
- Used with strongly-typed event payloads.

## Running the Demo

1. Start the application.
2. Navigate to the **Task Board** demo.
3. Click "Complete" on tasks to see:
   - The task row disappears (Handler 1).
   - The stats counter updates (Handler 2).
   - The activity log adds an entry (Handler 3).
   - A toast notification appears (Handler 4).
4. Click "Reset Demo" to restore the tasks.

## Code Highlights

- `Events/TaskEvents.cs`: Strongly-typed event payloads (`TaskCompletedEvent`).
- `Handlers/TaskBoardHandlers.cs`: Distributed handlers (`TaskListHandler`, `TaskStatsHandler`, `ActivityLogHandler`).
- `Controllers/TaskBoardController.cs`: Triggers `TaskEvents.Task.Completed` without knowing about UI updates.
- `Views/TaskBoard/Index.cshtml`: Main view with HTMX attributes and client-side event listeners.
- `Views/TaskBoard/_TaskRow.cshtml`, `_Stats.cshtml`, `_LogEntry.cshtml`: Partial views updated via OOB swaps.