# SwapLab

> **Interactive Pattern Library for Swap.Htmx**

SwapLab is a hands-on demo application that showcases all major Swap.Htmx patterns with working examples you can interact with and copy.

## Running SwapLab

```bash
cd demo/SwapLab
dotnet run
```

Then open https://localhost:5001 in your browser.

## What's Inside

### 🎯 Pattern Library

Each pattern includes:
- **Live Demo** - Working interactive example
- **Code View** - See the Razor view and controller code
- **Explanation** - Why this pattern works
- **Copy Button** - Get the code for your project

### Patterns Covered

| Pattern | Description |
|---------|-------------|
| **Basic Swap** | Simple GET/POST with partial updates |
| **Multi-Component** | Tabs + Search + Pagination + Grid |
| **Event Chains** | Cascading updates with `ISwapEventConfiguration` |
| **State Management** | Hidden fields, URL params, data attributes |
| **URL Sync** | Bookmarkable state with `SyncToUrl` property |
| **Conditional Swaps** | `AlsoUpdateIfExists()` and `AlsoUpdateIf()` for safe OOB swaps |
| **OOB Swaps** | Out-of-band updates with `AlsoUpdate()` |
| **Toasts** | Success, error, and custom notifications |
| **Modals** | Modal dialogs with form handling |
| **Infinite Scroll** | Lazy loading with `hx-trigger="revealed"` |
| **Search Debounce** | Real-time search with debouncing |
| **Form Validation** | Server-side validation with inline errors |
| **Source Generators** | Type-safe view paths, element IDs, and events |

### 🛠️ DevTools

SwapLab has DevTools enabled by default so you can see:
- Event timeline in the floating panel
- Console logging of all HTMX events
- State inspection for debugging

## Project Structure

```
SwapLab/
├── Controllers/
│   ├── HomeController.cs      # Main navigation
│   ├── PatternsController.cs  # Pattern demos
│   └── LabController.cs       # Interactive lab
├── Events/
│   └── SwapLabEvents.cs       # Event configurations
├── Models/
│   └── *.cs                   # View models
├── Views/
│   ├── Home/
│   ├── Patterns/
│   │   ├── _BasicSwap.cshtml
│   │   ├── _MultiComponent.cshtml
│   │   └── ...
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   └── _CodeBlock.cshtml
│   └── Lab/
└── wwwroot/
    ├── css/
    └── js/
```

## Learning Path

1. **Start with Basic Swap** - Understand the fundamentals
2. **Move to Event Chains** - See how events cascade
3. **Try Multi-Component** - The most common real-world pattern
4. **Explore State Management** - Where to store state
5. **Use DevTools** - Debug with the built-in tools

## Contributing

Found a pattern that should be included? Open a PR!
