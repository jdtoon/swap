# Modular Monolith Orchestration (MVC + HTMX)

This document describes how the host and modules collaborate in the modular monolith, how server-side event chains relate to the UI event system, and the preferred MVC patterns (Controllers + Partial Views vs View Components). It aligns with the swap-layered approach: .NET MVC + HTMX, Tailwind, and DaisyUI.

## Goals

- Host provides the shell and front-end pipeline (Tailwind, DaisyUI, HTMX, LibMan/npm) and composes module UI.
- Modules own their domain logic, contracts, server-side event chains, and UI fragments (Razor).
- UI refresh is declarative via HTMX events emitted by the server; no string HTML in code.
- Keep event systems clear: server event chaining vs client UI signaling.

## Responsibilities

- Host (MVC shell)
  - Layout, navigation, and shared assets: `_Layout.cshtml`, Tailwind/DaisyUI/HTMX includes.
  - Build asset pipeline (LibMan/npm) and static asset hosting.
  - Module discovery and lifecycle: `AddSwapModules`, `MapSwapModuleEndpoints`, `ConfigureSwapModuleEventChains`.
  - MVC wiring for module views via ApplicationParts/Razor Class Library (RCL).

- Modules
  - Services, domain state, projections, and Contracts (types/interfaces).
  - Server-side event chains in `IModule.ConfigureEventChains(IEventChainRegistrar registrar)`.
  - UI as Razor artifacts (Partial Views or View Components) and HTMX endpoints that return partials.
  - Emit UI events (HX-Trigger headers) when module state changes so the browser refreshes module panels.

## Event systems

- Server event chains (module-to-module)
  - API: `IEventChainRegistrar` (register typed handlers, publish events).
  - Purpose: Internal orchestration and projection updates. Example: `Orders.OrderCreated` → Inventory increments a counter.
  - Suggested infrastructure: an in-process registrar implementation owned by the framework and registered by the host.

- UI event bus (client signaling)
  - API: Swap.Htmx helpers that set HX-Trigger headers (e.g., `HxTrigger`, `HxTriggerAfterSwap`, typed overloads).
  - Purpose: Tell the browser to refresh specific fragments when server-side work completes.
  - Typical pattern: the module that updates its own state also emits its UI refresh event key (e.g., `ui.inventory.refresh`). The host page listens with `hx-trigger="ui.inventory.refresh from:body"` on that module’s DOM root.

These are orthogonal: the server chain updates state; the UI event tells the client to re-fetch the fragment.

## Where chains live

- Prefer chains defined by the module that cares about the event.
  - Example: Inventory subscribes to `Orders.OrderCreated` in `ConfigureEventChains`, updates its projection, and emits `ui.inventory.refresh` via Swap.Htmx.
- Host simply invokes `ConfigureSwapModuleEventChains` at startup to let all modules register their chains; it does not define the chains.

## Module UI patterns

Two recommended patterns for module-owned UI. Both avoid string HTML and use .cshtml.

### Option A: Controllers + Partial Views

- Each module ships MVC Controllers returning `PartialView` results for HTMX swaps.
- Endpoints are routable by default; HTMX `hx-get`/`hx-post` target these actions.
- Host composes initial page by either:
  - Server-side rendering of partials via a composite action (returns a single shell view that includes partials), or
  - Client-side HTMX `hx-get` on load to fetch module panels lazily.

Pros
- Simple and familiar MVC pattern.
- Great for HTMX: actions can return only the fragment that needs swapping.
- Easy to test with MVC action tests.

Cons
- Composition from the host may involve more wiring for initial SSR unless you build a composite controller per page.

Use when
- You prefer explicit routes for each fragment and a straightforward HTMX flow.

### Option B: View Components (+ HTMX endpoints)

- Each module exposes View Components (e.g., `InventoryPanelViewComponent`) with strongly-typed Razor views living in the module (RCL).
- Host composes the initial page using `<vc:inventory-panel />` etc.
- For updates, add a thin controller action that renders the View Component and returns its partial markup for HTMX swaps.

Pros
- Strong encapsulation: clear “widget” ownership by the module.
- Great for reusability and SSR composition in the host.
- Strong unit testing story for components.

Cons
- Requires a tiny action per component (or a helper) to expose an HTMX-friendly endpoint.

Use when
- You want encapsulated, reusable UI units and clean host composition.

### Recommendation

Adopt View Components for initial SSR composition, and pair each component with a small controller action to return the component’s markup for HTMX swaps. Modules still own the HTMX event key and endpoints. Controllers + partials remain valid for simple cases or APIs that don’t map to a component.

## Testing strategy

- Unit tests
  - Services, projections, and event handlers: pure C# tests; fake the `IEventChainRegistrar` as needed.
  - View Components and Partial Views: render with an MVC test host and assert generated HTML.

- Integration tests (use `Swap.Testing`)
  - Spin up a lightweight test host with the module(s) under test using ApplicationParts to include module assemblies.
  - Exercise HTMX endpoints; assert response markup and HX-Trigger headers.
  - Cross-module: trigger Orders create → assert Inventory projection and the `ui.inventory.refresh` header appears → fetch the Inventory partial and verify updated HTML.

## Infrastructure placement and naming

- Move the current demo’s `SimpleEventChainRegistrar` into the framework as a default in-process implementation (e.g., `Swap.Modularity.Eventing.InProcessEventChainRegistrar`).
- Register it in the host via DI. This does not change the UI event system; it remains orthogonal and provided by Swap.Htmx.
- Keep `IModule.ConfigureEventChains` as the module hook for chain registration (no extra interface required). Modules can organize chain registrations in dedicated files inside their project for clarity.

## Swap-layered alignment

- Host: MVC shell with Tailwind/DaisyUI/HTMX, npm/LibMan setup, and all registration (`AddSwapModules`, `MapSwapModuleEndpoints`, `ConfigureSwapModuleEventChains`).
- Modules: Razor Components (View Components or partials), HTMX endpoints, server-side chains, and event emission for UI refresh.
- Client wiring: Host layout includes `hx-trigger="<module-ui-event> from:body"` on each module panel root, so when the module emits its UI event, HTMX refreshes that fragment.

## Migration notes for the demo (no code changes yet)

- Introduce MVC in the host and set `_Layout.cshtml` with Tailwind/DaisyUI/HTMX.
- Convert module string responses to Razor:
  - Extract Inventory dashboard into a View Component + view.
  - Add an action to return the component’s markup for HTMX refresh.
  - Do the same for Orders panel if applicable.
- Keep event chains in modules. After handling domain events, modules emit their UI event keys via Swap.Htmx.
- Add integration tests using `Swap.Testing` to lock the behavior.

---

This keeps orchestration clear: the host composes and provides infra; each module owns its chains, UI, and refresh semantics. Server event chains remain independent from UI events, and HTMX drives the client-side updates declaratively.
