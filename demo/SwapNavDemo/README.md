# SwapNavDemo

Demonstrates the `.WithNavigation()` feature for SPA-style navigation in Swap.Htmx.

## Features Demonstrated

1. **Basic SPA Navigation** - Navigate without full page reload
2. **Custom Target** - Navigate to specific page regions
3. **Push URL Control** - Navigate with or without URL history
4. **Navigation with Toasts** - Combine navigation with notifications
5. **Advanced HxLocationOptions** - Full control over HX-Location

## Running

```bash
dotnet run
```

Then open http://localhost:5000

## Key Patterns

### Simple Navigation
```csharp
return this.SwapResponse()
    .WithNavigation("/inbox")
    .Build();
```

### Navigation to Custom Target
```csharp
return this.SwapResponse()
    .WithNavigation("/settings", target: "#sidebar")
    .Build();
```

### Navigation without URL Push (Modal Content)
```csharp
return this.SwapResponse()
    .WithNavigation("/modal-content", pushUrl: false)
    .Build();
```

### Navigation with Full Options
```csharp
return this.SwapResponse()
    .WithNavigation(new HxLocationOptions
    {
        Path = "/data",
        Target = "#grid",
        Swap = "innerHTML",
        Values = new() { ["filter"] = "active" }
    })
    .Build();
```
