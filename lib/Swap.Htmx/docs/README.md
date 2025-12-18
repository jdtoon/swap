# Swap.Htmx Documentation

Welcome to the Swap.Htmx documentation. These guides will help you build reactive, event-driven web applications with ASP.NET Core and HTMX.

---

## Quick Links

| I want to... | Read this |
|--------------|-----------|
| Get started with Swap.Htmx | [Getting Started](GettingStarted.md) |
| Understand what APIs are stable | [Public API & Compatibility](PublicApiAndCompatibility.md) |
| Secure my app (CSRF/SSE/Auth) | [Security Checklist](SecurityChecklist.md) |
| Clean navigation without boilerplate | [`<swap-nav>` Tag Helper](SwapNavTagHelper.md) ⭐ NEW |
| Navigate programmatically with toasts | [Navigation](Navigation.md) |
| Understand `SwapController` sharp edges | [SwapController Sharp Edges](SwapControllerSharpEdges.md) |
| Auto-generate view/element constants | [Auto-Scan Generator](AutoScanGenerator.md) ⭐ NEW |
| Migrate an existing MVC app | [Migration Guide](MigrationGuide.md) |
| Build complex multi-component UIs | [Multi-Component Coordination](MultiComponentCoordination.md) |
| Use strongly-typed state | [SwapState Guide](SwapState.md) |
| Copy-paste common patterns | [Recipes](Recipes.md) |
| Handle form validation | [Validation Guide](Validation.md) |
| Show success toasts for CRUD | [CRUD Toasts](CrudToasts.md) |
| Understand state patterns | [State Management](StateManagement.md) |
| Avoid common mistakes | [Anti-Patterns](AntiPatterns.md) |
| Pick a client assets strategy | [Client Assets](ClientAssets.md) |
| See the pinned HTMX versions | [Recommended Versions](RecommendedVersions.md) |
| Use events and triggers | [Events Guide](Events.md) |
| Avoid breaking trigger payloads | [Typed Payloads](TypedPayloads.md) |
| Understand event naming and realtime routing | [Event Naming & Realtime Routing](EventNamingAndRouting.md) |
| Set up real-time updates | [Realtime Overview](Realtime.md) |
| Understand realtime lifecycle & guarantees | [Realtime Bridge Behavior](RealtimeBridgeBehavior.md) |

---

## Core Concepts

These guides cover the fundamental patterns for building Swap.Htmx applications:

### [Getting Started](GettingStarted.md)
Step-by-step guide to your first Swap.Htmx application. Covers installation, configuration, and basic usage.

### [`<swap-nav>` Tag Helper](SwapNavTagHelper.md) ⭐ NEW
SPA-style navigation without the boilerplate. Write `<swap-nav to="/products">Products</swap-nav>` instead of verbose HTMX attributes. Includes auto-layout suppression for seamless partial updates.

### [Navigation](Navigation.md)
Programmatic navigation with `.WithNavigation()`. Navigate users while preserving toasts and triggers using the `HX-Location` header.

### [Migration Guide](MigrationGuide.md)
How to incrementally migrate an existing ASP.NET Core MVC application to use Swap.Htmx for partial-based UI updates.

### [Multi-Component Coordination](MultiComponentCoordination.md)
The definitive guide to building pages with multiple interactive components (tabs, search, pagination, grids) that work together through events and shared state.

### [SwapState Guide](SwapState.md)
First-class state management with strongly-typed classes, automatic model binding via `[FromSwapState]`, and OOB updates via `.WithState()`.

### [Recipes](Recipes.md)
Copy-paste patterns for common UI scenarios: filterable lists, multi-select pickers, split-view builders, inline edit, modals, wizards, and more.

### [State Management](StateManagement.md)
Where should state live? Hidden fields, URL parameters, session, data attributes? This guide covers all the patterns including SwapState.

### [Anti-Patterns](AntiPatterns.md)
Learn from others' mistakes. Common pitfalls and how to avoid them when building Swap.Htmx applications.

---

## Forms & Validation

### [Validation Guide](Validation.md) ⭐ NEW
Server-side validation with inline error display. Use `<swap-validation>` tag helper and `SwapValidationErrors()` for seamless form validation.

### [CRUD Toasts](CrudToasts.md) ⭐ NEW
Standard success messages for create, update, delete operations with `.WithCreatedToast()`, `.WithUpdatedToast()`, `.WithDeletedToast()`.

---

## Events & Updates

### [Events Guide](Events.md)
Type-safe event keys, triggering events, and the `HX-Trigger` header mechanics.

### [Event Naming & Realtime Routing](EventNamingAndRouting.md)
How event names map from `HX-Trigger` to realtime broadcasts (and what the `sse:` prefix actually means).

### [Event Chains](EventChains.md)
Centralize "when X happens, update Y" logic with `ISwapEventConfiguration` and distributed handlers.

### [Out-of-Band Swaps](OutOfBandSwaps.md)
Update multiple elements from a single response using OOB swaps and the `AlsoUpdate()` API.

---

## Realtime

### [Realtime Overview](Realtime.md)
Introduction to real-time HTML streaming with Swap.Htmx.

### [Realtime Bridge Behavior](RealtimeBridgeBehavior.md)
When realtime forwarding runs, what gets forwarded, and what happens on failures.

### [Server-Sent Events](ServerSentEvents.md)
Push updates from server to browser using SSE, with room-based broadcasting and connection management.

### [SSE Backpressure](SseBackpressure.md)
Configure max event size, per-connection queue limits, and drop strategy.

### [WebSockets](WebSockets.md)
Two-way real-time communication for interactive features.

### [Redis Backplane](RedisBackplane.md)
Scale SSE/WebSocket across multiple servers using Redis as a message backplane.

---

## Framework Integration

### [Minimal APIs](MinimalApis.md)
Use `SwapResults` to return Swap responses from Minimal API endpoints.

### [Razor Pages](RazorPages.md)
Integrate Swap.Htmx with `PageModel` using extension methods.

### [Auto-Scan Generator](AutoScanGenerator.md) ⭐ NEW
Zero-configuration generation of `SwapViews` and `SwapElements` constants. Just add `<AdditionalFiles>` to your `.csproj` — no attributes needed.

### [Source Generators](SourceGenerators.md)
Attribute-based generation with `[SwapEventSource]`, `[SwapViewSource]`, and `[SwapElementSource]` for custom naming or selective generation.

### [User Context](UserContext.md)
Customize how Swap.Htmx resolves user identity for session management and real-time targeting.

---

## Development

### [Debugging & Logging](DebuggingAndLogging.md)
Troubleshooting tips, log configuration, and the dev endpoints.

---

## Learning Path

### New to Swap.Htmx?

1. **Start here:** [Getting Started](GettingStarted.md)
2. **Understand events:** [Events Guide](Events.md)
3. **Build complex UIs:** [Multi-Component Coordination](MultiComponentCoordination.md)
4. **Avoid pitfalls:** [Anti-Patterns](AntiPatterns.md)

### Migrating an Existing App?

1. **Plan the migration:** [Migration Guide](MigrationGuide.md)
2. **Understand state:** [State Management](StateManagement.md)
3. **Set up events:** [Events Guide](Events.md)
4. **Use OOB swaps:** [Out-of-Band Swaps](OutOfBandSwaps.md)

### Building Real-Time Features?

1. **Overview:** [Realtime Overview](Realtime.md)
2. **Choose approach:** [SSE](ServerSentEvents.md) or [WebSockets](WebSockets.md)
3. **Scale out:** [Redis Backplane](RedisBackplane.md)

---

## Demo Applications

The repository includes several demo applications showing different patterns:

| Demo | Description |
|------|-------------|
| `SwapMinimal` | Minimal API + Swap.Htmx basics |
| `SwapNavDemo` | Navigation with `.WithNavigation()` ⭐ NEW |
| `SwapStateDemo` | Server-driven state management with `<swap-state>` |
| `SwapShop` | E-commerce with cart, products, events |
| `TaskFlow` | Task management with real-time updates |
| `SwapWebSockets` | WebSocket integration example |
| `SwapRedisDemo` | Multi-server SSE with Redis |
| `SwapPages` | Razor Pages integration |

---

## Need Help?

- Check the [Anti-Patterns Guide](AntiPatterns.md) for common mistakes
- Look at the demo applications for working examples
- Review the [main README](../README.md) for quick reference
- See the [ROADMAP](../../../ROADMAP.md) for upcoming features
