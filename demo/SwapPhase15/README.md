# SwapPhase15 Demo

This demo showcases the new features introduced in Phase 1.5 of Swap.Htmx:

## Features Demonstrated

### 1. Distributed UI Handlers (`ISwapEventHandler<T>`)
- Handlers are automatically discovered via assembly scanning.
- Decoupled event processing without central configuration.
- Priority-based execution ordering.

### 2. Auto-Validation Filter (`[SwapForm]`)
- Automatic model validation for HTMX requests.
- Returns validation errors without boilerplate code.
- Reduces repetitive `if (!ModelState.IsValid)` checks.

### 3. Client Actions Protocol
- Declarative client-side actions like focus, reset, scroll.
- Triggered from server responses.
- Extensible for custom actions.

## Running the Demo

1. Start the application.
2. Submit the form with a message - see validation and handler execution.
3. Click the button to trigger events and client actions.

## Code Highlights

- `Handlers/UserClickedHandler.cs`: Example distributed handler.
- `Controllers/HomeController.cs`: Uses `[SwapForm]` and triggers events.
- `Views/Home/Index.cshtml`: Form with HTMX and event listening.</content>
<parameter name="filePath">c:\jd\swap\demo\SwapPhase15\README.md