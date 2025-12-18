# Changelog

All notable changes to Swap.Htmx will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-12-XX

### Added
- New packages: `Swap.Htmx.Realtime` and `Swap.Htmx.Realtime.Redis`.

### Breaking
- Realtime features (SSE/WebSockets, connection registry, event bridge, middleware) moved out of `Swap.Htmx` into `Swap.Htmx.Realtime`.
- Redis backplane support moved out of `Swap.Htmx` into `Swap.Htmx.Realtime.Redis`.
- `SwapController` no longer includes the protected `ServerSentEvents(...)` helpers. Use `SwapRealtimeController` from `Swap.Htmx.Realtime` for controller-based SSE helpers.
- Minimal API helpers `SwapResults.Sse(...)` / `SwapResults.WebSocket(...)` were removed from `Swap.Htmx`. Use `SwapRealtimeResults.Sse(...)` / `SwapRealtimeResults.WebSocket(...)` from `Swap.Htmx.Realtime`.

## [1.0.4] - 2025-01-XX

### Removed

#### JavaScript
- **Removed `swap-include-state`** - This JavaScript attribute auto-expanded to `hx-include`. Use standard `hx-include="#state-id"` instead. The standard HTMX attribute is clearer and doesn't require custom JS.

### Changed

#### Internal Refactoring
- **Consolidated `RenderStateAsOob`** - Moved duplicate state rendering logic from `SwapResult`, `SwapPageResult`, and `SwapActionResult` into shared `SwapStateRenderer` helper.

---

## [1.0.3] - 2025-12-09

### Changed

#### SwapState Model Binder
- **Fixed duplicate field handling** - When form inputs and hidden fields have the same name, the model binder now correctly uses the last value (visible input wins over hidden field)
- This enables the proper pattern where `<swap-state>` hidden fields provide defaults, and visible form inputs override them

#### Tag Helpers
- **Removed `data-swap-state` attribute** - This attribute on state containers served no purpose. DevTools now uses the `[id$="-state"]` CSS selector convention instead.

#### Documentation
- **New `llms.txt`** - Complete rewrite focused on SwapState pattern with clear examples
- **SwapStateDemo README** - Rewritten with correct patterns
- **OOB Pattern documented** - Added clear guidance on when to use `.WithState()` for OOB updates vs. swapping entire content

### Removed

#### Tag Helpers
- **Removed `<swap-hidden>`** - This tag helper added no value over plain `<input type="hidden">` and was confusing alongside `<swap-state>`. Use `<swap-state>` for state management instead.

#### Source Generators
- **Removed `StateClassGenerator`** - The `[SwapStateSource]` attribute and `swap-state-prop` view annotations have been removed. Define SwapState classes directly in C# where you get IntelliSense, validation attributes, and proper tooling support.

---

## [1.0.2] - 2025-XX-XX

### Added

#### `<swap-nav>` Tag Helper (NEW)
- **`<swap-nav to="/path">`** - Simplified SPA-style navigation links
- Automatically renders `<a>` with `hx-get`, `hx-target`, and `hx-push-url`
- Configurable default target via `SwapHtmxOptions.DefaultNavigationTarget`
- `push-url` attribute to control browser history (default: `true`)
- Passes through all HTML attributes (class, style, etc.) and `hx-*` attributes

#### Auto-Layout Suppression (NEW)
- **`SwapHtmxOptions.AutoSuppressLayout`** - Automatically suppress layout for HTMX requests
- **`Context.ShouldSuppressLayout()`** - Extension method for `_ViewStart.cshtml`
- Eliminates per-module `_ViewStart.cshtml` files
- HTMX requests automatically return partials, browser requests return full pages

#### Auto-Scan Source Generator (NEW)
- **Zero-configuration generation** of `SwapViews` and `SwapElements` constants
- Scans all `.cshtml` files via `<AdditionalFiles>` — no attributes required
- Generates nested classes matching folder structure
- Supports modular monolith structure (`Modules/*/Views/...`)
- Extracts all `id="..."` attributes automatically

#### Documentation
- [SwapNavTagHelper.md](lib/Swap.Htmx/docs/SwapNavTagHelper.md) - Complete `<swap-nav>` guide
- [AutoScanGenerator.md](lib/Swap.Htmx/docs/AutoScanGenerator.md) - Zero-config generator guide

### Changed

#### SwapNavDemo
- Updated to demonstrate `<swap-nav>` tag helper (previously showed `.WithNavigation()`)

#### Swap.ModularMonolith Template
- Uses `<swap-nav>` for navigation links in layout
- Configured with `AutoSuppressLayout = true`
- Uses auto-generated constants (no `ViewSources.cs` or `ElementSources.cs` needed)
- Simplified `_ViewStart.cshtml` with `ShouldSuppressLayout()` check

### Upgrading from 1.0.x

**No breaking changes.** All new features are opt-in and additive. To adopt:

1. **`<swap-nav>` tag helper** — Replace verbose navigation links:
   ```html
   <!-- Before -->
   <a href="/products" hx-get="/products" hx-target="#main-content" hx-push-url="true">Products</a>
   
   <!-- After -->
   <swap-nav to="/products">Products</swap-nav>
   ```
   Configure target in `Program.cs`:
   ```csharp
   builder.Services.AddSwapHtmx(options => options.DefaultNavigationTarget = "#main-content");
   ```

2. **Auto-layout suppression** — Eliminate per-folder `_ViewStart.cshtml` files:
   ```csharp
   // Program.cs
   builder.Services.AddSwapHtmx(options => options.AutoSuppressLayout = true);
   ```
   ```razor
   // _ViewStart.cshtml
   @{ Layout = Context.ShouldSuppressLayout() ? null : "_Layout"; }
   ```

3. **Auto-scan generator** — Delete manual `ViewSources.cs`/`ElementSources.cs`, add to `.csproj`:
   ```xml
   <AdditionalFiles Include="Views\**\*.cshtml" />
   ```
   Use generated `SwapViews.Home.Index` and `SwapElements.Home.Index.MainContent` constants.

---

## [1.0.1] - 2025-12-02

### Added

#### Navigation (NEW)
- **`.WithNavigation(path)`** - SPA-style navigation via `HX-Location` header
- **`.WithNavigation(path, target, swap)`** - Navigate with target element and swap mode
- **`NavigationOptions`** record for full control over navigation behavior
- Navigate while preserving toasts and triggers (unlike redirects which lose headers)

#### Tag Helpers
- **`<swap-hidden>`** tag helper for hidden form fields with auto-formatting
  - DateTime → `yyyy-MM-dd` format
  - bool → lowercase `true`/`false`
  - Collections → comma-separated values
  - Custom date formats via `date-format` attribute

#### Demo Applications
- **SwapNavDemo** - Navigation patterns with `.WithNavigation()`
- **SwapStateDemo** - Hidden field state patterns

### Documentation
- Added [Navigation Guide](lib/Swap.Htmx/docs/Navigation.md)
- Updated main README with navigation examples
- Updated docs index with new guides and demos

---

## [1.0.0] - 2025-11-27

### 🎉 First Stable Release

This is the first stable release of Swap.Htmx. The API surface has been reviewed and frozen for v1.x compatibility.

### Added

#### State Management (v0.13.0)
- **SwapState** base class for strongly-typed state containers
- **[FromSwapState]** attribute for automatic model binding from hidden fields
- **`<swap-state>`** tag helper for rendering state containers
- **Html.SwapStateContainer()** extension for rendering state
- **`.WithState(state)`** on SwapResponseBuilder for OOB state updates
- **URL Sync** support via `UrlSync` property on SwapState
- **Change Tracking** via `ChangedProperties` and `HasChanges`

#### Validation (v0.14.0)
- **`<swap-validation for="PropertyName" />`** tag helper for inline error display
- **`SwapValidationErrors()`** extension for returning validation errors
- **`SwapValidationErrorsOob()`** for OOB validation error updates  
- **`WithValidationErrors()`** on SwapResponseBuilder
- **`ClearValidationErrors()`** for clearing previous errors

#### CRUD Toasts (v0.14.0)
- **`WithCrudToast(operation, entityName, itemName)`** for standardized messages
- **`WithCreatedToast(entityName, itemName)`** - "Product 'Widget' created"
- **`WithUpdatedToast(entityName, itemName)`** - "Product 'Widget' updated"
- **`WithDeletedToast(entityName, itemName)`** - "Product 'Widget' deleted"
- **`WithSavedToast(entityName, itemName)`** - "Product 'Widget' saved"

#### State Coordination JS (v0.14.0)
- **`swap-include-state`** attribute auto-expands to `hx-include`
- Simplifies including state containers in HTMX requests
- Supports multiple states: `swap-include-state="state1, state2"`

#### Shortcuts (v0.14.0)
- **`this.SwapRedirect(url)`** for post-action redirects
- **`this.SwapRedirect(url, message)`** with toast message

#### Source Generators (v0.13.0)
- **ElementIdGenerator** - Generates strongly-typed element ID constants from .cshtml files
- **ViewPathGenerator** - Generates view path constants for compile-time safety
- **StateClassGenerator** - Generates SwapState classes from view markup
- **HandlerValidationAnalyzer** - Warns on unhandled events and missing handlers

#### Realtime Enhancements (v0.11.2+)
- **In-memory SSE backplane** for single-server deployments
- **Redis backplane** for multi-server SSE scaling
- **WebSocket support** with connection registry
- **Broadcast filtering** by room, user, role, or custom predicate

### Changed

#### Internal API Cleanup (v1.0.0)
- `SwapEventHandlerExecutor` - Now internal (was public)
- `SwapEventHandlerRegistry` - Now internal (was public)
- `SwapDiagnostics` - Now internal, use `ISwapDiagnostics` interface
- `SwapEventService` - Now internal, use `ISwapEventService` interface
- `SseFallbackExampleController` - Now internal (dev example only)

#### OOB Swap Rendering (v0.12.0)
- Fixed OOB content not being appended to response
- Fixed `<tr>` elements being incorrectly wrapped in `<div>`
- OOB swaps now detect if partial has target ID and add `hx-swap-oob` attribute instead of wrapping

### Fixed

- **OOB Swaps Not Rendering** - OOB content was stored in ViewData but never appended to response
- **OOB Element Wrapping** - Framework wrapped all OOB content in `<div>` breaking `<tr>` elements
- **DevTools Tab Switch** - Timeline stopped recording after switching tabs
- **InvariantCulture Decimals** - Price parsing failed across cultures (1299.99 vs 1299,99)

### Migration Guide

#### From v0.11.x to v1.0.0

1. **Internal Classes**: If you were directly instantiating `SwapEventHandlerExecutor`, `SwapEventHandlerRegistry`, `SwapDiagnostics`, or `SwapEventService`, use the corresponding interfaces instead (`ISwapDiagnostics`, `ISwapEventService`). These are automatically registered via DI.

2. **State Management**: Consider migrating from manual hidden field management to SwapState:
   ```csharp
   // Before: Manual hidden fields
   <input type="hidden" name="tab" value="@Model.Tab" />
   <input type="hidden" name="page" value="@Model.Page" />
   
   // After: SwapState
   <swap-state state="Model.State" />
   ```

3. **Toast Messages**: Consider using the new CRUD toast presets for consistent messaging:
   ```csharp
   // Before
   .WithSuccessToast("Product updated successfully")
   
   // After
   .WithUpdatedToast("Product", product.Name)
   ```

---

## [0.14.0] - 2025-XX-XX

### Added
- Validation tag helper and extensions
- CRUD toast presets
- swap-include-state JS behavior
- SwapRedirect shorthand

## [0.13.0] - 2025-XX-XX

### Added
- SwapState system for strongly-typed state management
- Source generators for element IDs, view paths, state classes
- Handler validation analyzer

## [0.12.0] - 2025-XX-XX

### Added
- SwapLab demo application with 15 patterns
- Development diagnostics and DevTools panel
- Comprehensive documentation (7 guides)

### Fixed
- OOB swap rendering issues
- DevTools tab switching

## [0.11.4] - 2024-XX-XX

### Added
- InvariantDecimalModelBinder for cross-culture support

## [0.11.3] - 2024-XX-XX

### Added
- Redis backplane for SSE scaling
- In-memory SSE backplane

## [0.11.2] - 2024-XX-XX

### Added
- Distributed event handling
- Auto-validation for HTMX requests

## [0.11.1] - 2024-XX-XX

### Fixed
- NuGet package source generator inclusion
