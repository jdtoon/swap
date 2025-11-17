# Debugging and Logging in Swap.Htmx

## Development Logging

Swap.Htmx includes comprehensive debug logging to help you understand what's happening under the hood during development.

### Enabling Debug Logs

**Option 1: Using appsettings.json (Recommended)**

Add the following to your `appsettings.Development.json`:

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

This integrates with ASP.NET Core's ILogger infrastructure and respects all standard logging configuration.

**Option 2: Console-Only Output (Quick Testing)**

Set an environment variable:

```powershell
# PowerShell
$env:SWAP_DEV_LOGGING="true"

# Bash
export SWAP_DEV_LOGGING=true
```

Then run your application. This outputs colored console logs without requiring logger configuration.

### What Gets Logged

When debug logging is enabled, you'll see detailed information about:

#### Event Chain Execution
```
[SwapEvent] Event: cart.itemAdded, Payload: Cart
[SwapEvent] Executor found: True
[EventChainExecutor] Event: cart.itemAdded, Partials: 1, Toasts: 1
[SwapEvent] Executor returned: True
```

#### Toast Application
```
[SwapActionResult] Applying toast: Success - Item added to cart
```

#### HTTP Headers
```
[SwapActionResult] HX-Trigger (before render): {"showToast":{"type":"success","message":"Item added to cart"}}
[SwapActionResult] HX-Trigger (after render): {"showToast":{...},"cart.itemAdded":{...}}
```

### Log Categories

All Swap.Htmx logs use the category `Swap.Htmx`, so you can control them independently:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Swap.Htmx": "Debug"  // Only debug Swap.Htmx
    }
  }
}
```

### Best Practices

1. **Development Only**: Debug logs are compiled out in Release builds (via `[Conditional("DEBUG")]`)
2. **Structured Logging**: Logs include structured properties for easy filtering
3. **Color Coding**: Console output uses colors for easy visual scanning
   - **Cyan**: Event chain execution
   - **Yellow**: Toast operations
   - **Green**: HTTP headers

### Production Considerations

Debug logging is automatically disabled in production:
- Compiled out via `[Conditional("DEBUG")]` attribute
- No performance impact
- No sensitive data exposure risk

### Troubleshooting Guide

**Toasts not showing?**
1. Check logs for: `[SwapActionResult] Applying toast: Success - ...`
2. Verify HX-Trigger header contains `showToast` event
3. Check browser console for JavaScript errors

**Event chains not executing?**
1. Look for: `[EventChainExecutor] Event: {name}, Partials: X, Toasts: Y`
2. If you see "No chain configured", check EventChainConfiguration.cs
3. Verify event name matches exactly (case-sensitive)

**Headers being overwritten?**
1. Check the before/after HX-Trigger header logs
2. Multiple events should be merged in same JSON object
3. Example: `{"showToast":{...},"customEvent":{...}}`
