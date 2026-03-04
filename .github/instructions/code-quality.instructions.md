---
description: "Use when writing or modifying C# code in the Swap framework libraries (lib/ and framework/). Covers API design rules, dependency policy, the SwapResponseBuilder pattern, event system conventions, and public API contract requirements."
applyTo: ["lib/**/*.cs", "framework/**/*.cs"]
---
# Swap Framework Code Quality Standards

## Dependency Policy

- **Swap.Htmx**: Zero external NuGet dependencies. Framework references only (`Microsoft.AspNetCore.App`).
- **Swap.Testing**: Only `AngleSharp` and `Microsoft.AspNetCore.Mvc.Testing` allowed.
- **Swap.Htmx.Realtime**: May reference `Swap.Htmx` only.
- **Swap.Htmx.Realtime.Redis**: May reference `Swap.Htmx.Realtime` and `StackExchange.Redis`.

Adding a new external dependency requires explicit justification.

## Public API Contract

Every public type, method, or property is a contract. Changes must be intentional:

- **New public members**: Add snapshot test in `PublicApiSnapshots/`
- **Renamed members**: This is a breaking change — bump major version
- **Removed members**: This is a breaking change — bump major version
- **Parameter additions**: Use optional parameters with defaults to avoid breaks
- **Return type changes**: Breaking — bump major version

## API Design Patterns

### SwapResponseBuilder Fluent API
Every builder method must:
- Return `SwapResponseBuilder` for chaining
- Validate inputs eagerly (throw `ArgumentException` at call site, not at render time)
- Have XML doc comments with `<example>` usage

### Extension Methods
- Group by concern in separate `*Extensions.cs` files
- Place in the `Swap.Htmx` namespace (not a sub-namespace) for easy discovery
- Controller extensions go in `Extensions/SwapControllerExtensions.cs`

### Event System
- Use `EventKey` struct for all event identifiers — never raw strings in framework code
- `[SwapEventSource]` generates type-safe keys from `const string` fields
- Event names use dot notation: `"entity.action"` (e.g., `"patient.created"`)
- Event handlers implement `ISwapEventHandler<T>`

### SwapState
- Properties must be simple types (string, int, bool, enum) — no complex objects
- Change tracking is automatic — don't manually set `HasChanges`
- URL sync is opt-in via `[SwapStateUrl]` attribute

## Error Handling

- **Middleware errors**: Log and return HTMX-friendly error response (toast, not raw exception)
- **Builder validation errors**: Throw `ArgumentException` with clear message at call site
- **View rendering errors**: Catch in result execution, fall back to error toast via OOB
- **Never swallow exceptions silently** — log at minimum Warning level

## Multi-Target Framework

All libraries target `net8.0`, `net9.0`, `net10.0`. When using newer APIs:

```csharp
#if NET9_0_OR_GREATER
    // Use newer API
#else
    // Fallback for net8.0
#endif
```

## Performance

- Avoid allocations in hot paths (middleware, request pipeline)
- Use `ReadOnlySpan<char>` and `string.Create` where practical
- Cache view discovery results in production
- OOB swaps should render in parallel where possible
- Benchmark significant changes using `lib/Swap.Htmx.Benchmarks/`

## Security

- Validate OOB target IDs against injection (alphanumeric, hyphens, underscores only)
- HTML-encode all user-provided content in HX-Trigger headers
- Validate redirect/navigation URLs (block `javascript:`, `data:` schemes)
- SwapState supports encryption via `IDataProtection` — use for sensitive fields
