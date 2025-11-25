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
2. Submit the form with a message - see validation and handler execution.
3. Click the button to trigger events and client actions.
4. Increment the counter to see event chaining: counter updates and stats refresh simultaneously.

## Code Highlights

- `Events/UserClickedEvent.cs` & `CounterUpdatedEvent.cs`: Strongly-typed event payloads.
- `Handlers/UserClickedHandler.cs` & `CounterUpdatedHandler.cs`: Distributed handlers.
- `Events/AppEventConfig.cs`: Event chains for orchestration.
- `Controllers/HomeController.cs`: Uses `[SwapForm]` and triggers events with payloads.
- `Views/Home/Index.cshtml`: Form with HTMX, validation, and event listening.
- `Views/Home/_Counter.cshtml` & `_Stats.cshtml`: Partial views updated via event chains.</content>
<parameter name="filePath">c:\jd\swap\demo\SwapPhase15\README.md