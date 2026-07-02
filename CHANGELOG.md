# Changelog

All notable changes to Swap.Htmx will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.0] - 2026-07-02

**Client-orchestration + polish release.** Adds content-diff skipping, existence/optimistic guards, OOB
coalescing, presence auto-registration, and an analyzer code fix. Non-breaking (additive). Targets
net8.0/net9.0/net10.0.

### Added
- **Fingerprint diff-skip (`data-swap-hash`).** `AlsoUpdate(..., fingerprint: true)` /
  `AlsoMorph(..., fingerprint: true)` stamp the rendered fragment with a stable content hash; the client
  skips the swap when the new content matches what's already in the DOM — no needless re-render, no lost
  focus/scroll.
- **Client-enforced guarded swaps.** `AlsoUpdateIfExists` now emits `data-swap-if-exists`; the client
  skips the swap cleanly when the target isn't in the DOM (no stray element, no htmx no-target error).
- **Safe optimistic UI (`data-swap-optimistic`).** Request-scoped rollback: the client snapshots the
  target before the request and restores it on any failure (non-2xx / network / timeout), re-running
  `htmx.process` on the restored nodes — a rejected request never leaves an optimistic change stuck.
  Adds a `swap-pending` class for the request's duration.
- **OOB coalescing.** Duplicate replace-style OOB swaps to the same target collapse to the last one
  (fewer renders, no double-swap); insert-position swaps still accumulate.
- **Presence auto-registration.** `AddSseEventBridge` registers `IRealtimePresence` →
  `InMemoryRealtimePresence` via `TryAddSingleton` (a user's own registration still wins).
- **Analyzer code fix for `SWAP001`.** Scaffolds the missing `ISwapEventHandler<T>` implementation
  (collision-safe class name); ships in the analyzer package.

### Changed
- `<swap-scripts>` default CDN pins bumped to match RecommendedVersions: htmx 2.0.8, htmx-ext-sse 2.2.4.

### Notes
- `data-swap-hash`, `data-swap-if-exists`, and `data-swap-optimistic` require the Swap client runtime
  (auto-included by `<swap-scripts>`); the client guards were verified in-browser.

## [1.6.0] - 2026-07-02

**"Smart engine" release.** Adds the flagship dependency-graph orchestration, DOM morphing, frames,
flash toasts, realtime presence, multi-step flows, and a performance pass. Non-breaking (additive).
Targets net8.0/net9.0/net10.0.

### Added
- **Fragment dependency graph (flagship).** Register dependency-aware fragments once —
  `o.Fragments.Fragment("revenue", "_Revenue", ctx => …).DependsOn("orders")` — then invalidate a topic:
  `SwapResponse().WithView("_OrderRow", order).Invalidate("orders")`. The engine re-renders every
  fragment depending on the invalidated topics — deduplicated, and skipping any target already covered
  by an explicit `AlsoUpdate` — as OOB swaps. New: `SwapFragmentRegistry`, `SwapResponseBuilder.Invalidate`,
  `SwapHtmxOptions.Fragments`.
- **DOM morphing (idiomorph).** `SwapMode.MorphInner`/`MorphOuter` and `AlsoMorph(...)` emit
  `hx-swap-oob="morph:innerHTML|outerHTML"`, preserving focus, caret, scroll position and in-flight
  transitions instead of destructively replacing.
- **`<swap-scripts>`** tag helper — renders the client script block (htmx, optional idiomorph for morph,
  optional SSE extension, the Swap client, dev tools in Development); all sources overridable.
- **`<swap-frame>`** tag helper — Turbo-Frame-style lazy (`loading="lazy"`) / scoped navigation regions.
- **`<swap-upload>`** tag helper — file upload with a live `<progress>` bar wired to htmx `xhr:progress`.
- **`WithFlash(message, type)`** — flash toasts that survive an HTTP redirect (stashed in TempData,
  re-emitted as `HX-Trigger showToast` on the next response); the Minimal-API result emits immediately.
- **Out-of-order / duplicate-safe OOB (`data-swap-seq`).** `AlsoUpdate`/`AlsoMorph` accept an optional
  monotonic `seq` (e.g. a rowversion); a client `htmx:oobBeforeSwap` guard drops any swap that is not newer.
- **`IRealtimePresence`** (+ `InMemoryRealtimePresence`) — single-node who's-present-in-which-room tracking.
- **`SwapFlow`** — a server-authoritative multi-step flow/wizard step-machine (guards, bounds, restore).

### Changed / Performance
- **`SwapState` reflection is cached per Type**, eliminating `GetProperties()` + LINQ on every render/bind;
  the cached property set is exposed only as a read-only view so it cannot be mutated.
- **`AutoScanGenerator` is now incremental** — editing unrelated `.cs` files no longer re-runs the view scan.

### Notes
- Morphing and the `data-swap-seq` guard require the client scripts (`<swap-scripts>`); idiomorph loads
  from a CDN by default (override `idiomorph-src` to vendor it locally).

## [1.5.0] - 2026-07-01

**DX unlock release.** Ships the previously-omitted XML documentation, removes an onboarding
foot-gun, fixes the flagship template, and sharpens the analyzer. Non-breaking. Targets net8.0/9.0/10.0.

### Added
- **XML documentation now ships** in the NuGet packages (`GenerateDocumentationFile`), so consumer
  IntelliSense quick-info and NuGet metadata receive the authored doc comments — previously the Razor SDK
  left this off, so all of them were silently invisible.
- Analyzer rules now carry **help links** to the documentation.

### Fixed
- **`AddSwapHtmx(Action<SwapEventBusOptions>)` now fully configures the application.** It previously
  registered only a subset of services (notably not the `SwapHtmxOptions` singleton), so `UseSwapHtmx()`
  failed with a DI error at request time for anyone who wrote `AddSwapHtmx(e => e.When(...))`. It now
  delegates to the full registration, applying the event configuration to the shared `EventBus`.
- **The `Swap.Mvc` template** now registers the Swap tag helpers (`@addTagHelper *, Swap.Htmx`), so a
  scaffolded app renders `<swap-*>` tag helpers instead of emitting them as literal text.
- **Analyzer `SWAP001`** no longer false-positives on events dispatched to a typed `ISwapEventHandler<T>`;
  it matches the trigger's payload type against registered typed handlers.
- Fixed pre-existing XML-doc defects surfaced by enabling documentation (a malformed `<param>` tag, a
  `<param>` for a non-existent parameter, and missing param docs).

### Removed
- **Analyzer `SWAP003`** (potential circular event chain) — it was declared but never emitted (the
  detection was never implemented). Removed so the diagnostic set is trustworthy.

## [1.4.0] - 2026-07-01

**"Trust floor" release.** This is a security and concurrency-correctness release. It hardens the
exact features that are marketed as safety controls — tamper-proof `SwapState`, the error boundary,
and redirect guards — and fixes nondeterministic failures on the realtime and OOB-render hot paths.
It contains **behavior changes**; see *Migration* below. Applies to all targets: `net8.0`, `net9.0`, `net10.0`.

### Security
- **`[SwapProtected]` state no longer leaks as plaintext on Razor Pages.** `SwapPageResult` rendered
  protected `SwapState` without a data-protection provider, emitting protected values as plaintext
  hidden fields (and then failing to bind them back). All result types (MVC, Razor Pages, Minimal APIs)
  now render state through a single request-scoped helper (`SwapStateRenderer.RenderAsOobForRequest`)
  that always resolves the provider, and **throws** if a protected state is rendered with no
  `IDataProtectionProvider` registered rather than silently emitting plaintext.
- **Tamper protection now fails closed.** Previously a tampered, cleared, or missing `[SwapProtected]`
  value bound silently to the type default (e.g. a protected `decimal Price` → `0`, a protected
  `Guid TenantId` → `Guid.Empty`) with no signal. Now a protected value that is present but empty or
  fails to decrypt is a hard model-binding failure: the binder records a `ModelState` error, fails the
  bind, and sets the new `SwapState.Tampered` flag. `SwapState.FromQueryString(...)` also sets
  `Tampered` so direct callers can inspect it.
- **Error-boundary output is now HTML-encoded.** The built-in fallback error toast interpolated the
  exception message into raw HTML, allowing reflected XSS when exception details were shown. All
  interpolated values are HTML-encoded, the raw message is no longer echoed by default, and a request
  correlation id is shown while full details are logged server-side.
- **Redirect/navigation URLs are now validated with an allowlist.** `WithRedirect()` and
  `WithNavigation()` replace the previous scheme *blocklist* with an allowlist: only `http`/`https`
  absolute URLs and same-origin relative references are accepted. Protocol-relative URLs (`//evil.com`,
  `/\evil.com`) and non-http(s) schemes (`javascript:`, `data:`, `vbscript:`, `file:`, `mailto:`, …)
  are rejected. The same validation now also runs in the `WithNavigation(HxLocationOptions)` overload,
  which previously bypassed all checks.

### Fixed
- **SSE writes are serialized.** The SSE writer loop and the keep-alive heartbeat wrote to the same
  Kestrel response `PipeWriter` with no synchronization, which under load threw
  "Concurrent writes to the response body are not allowed" or interleaved frames the client mis-parsed.
  All writes now pass through a per-connection write lock in `ServerSentEventStream`.
- **SSE connections no longer become zombies.** When the writer loop exited via
  `OperationCanceledException` (e.g. a client dropped mid-send), it left the connection reporting
  `IsActive = true` with pending sends that never completed, hanging every subsequent broadcaster (and
  the unrelated request that triggered the flush). The loop now cancels the connection and drains its
  queue on exit for both the cancellation and fault paths.
- **Keep-alive is a real SSE comment.** `SwapSseResult`'s heartbeat sent a client-visible `ping` event
  with a `{}` body; it now sends a `: keepalive` comment.

### Changed
- **OOB swaps render sequentially (reverts the 1.3.0 parallelization).** 1.3.0 rendered out-of-band
  partials with `Task.WhenAll`. Because every partial renders on the single request scope (scoped
  `DbContext`, `ViewData`, the shared view-buffer pool), this raced those scoped services and
  intermittently threw "A second operation was started on this context instance". View rendering is
  CPU-bound string building, so sequential rendering removes the race at effectively no cost. Ordering
  is unchanged.
- **New public API:** `SwapState.Tampered` (get).

### Migration
- If you relied on a tampered/cleared `[SwapProtected]` value silently becoming the type default, that
  request now **fails binding**. Check `ModelState.IsValid` (or the new `SwapState.Tampered`) and handle
  the tamper explicitly.
- If you passed non-http(s) or protocol-relative URLs to `WithRedirect()`/`WithNavigation()`, those calls
  now throw `ArgumentException`. Use rooted relative paths (`/path`) or absolute `http`/`https` URLs.
- Protected state now requires `IDataProtectionProvider` to be registered (it is by `AddSwapHtmx()`);
  rendering protected state without it throws instead of leaking plaintext.
- No action needed for the OOB sequential-render change unless you depended on concurrent partial
  execution (which was unsafe).

## [1.3.0] - 2026-03-04

### Changed
- **Parallelized OOB Rendering** — Out-of-band swaps are now rendered in parallel using `Task.WhenAll()` instead of sequentially. This applies to all three result types: `SwapActionResult` (MVC), `SwapResult` (Minimal API), and `SwapPageResult` (Razor Pages). Ordering is preserved. Dashboards with many OOB swaps (e.g. 12–24 partials) see measurably faster response times.

### Added
- **OOB Target ID Validation** — `NormalizeOobTargetId()` now validates target IDs against `^[a-zA-Z][a-zA-Z0-9_-]*$` after stripping `#` and whitespace. Invalid IDs (empty strings, XSS payloads, IDs starting with numbers or containing special characters) throw `ArgumentException`. Affects `AlsoUpdate()`, `AlsoUpdateIfExists()`, `AlsoUpdateIf()`, and `AlsoUpdateMany()`.
- **Redirect/Navigation URL Validation** — `WithRedirect()` and `WithNavigation()` now reject URLs with `javascript:`, `data:`, and `vbscript:` schemes, preventing open redirect and XSS attacks via HX-Redirect and HX-Location headers.
- **Swap.Testing: Cookie Persistence** — `HtmxTestClient` now automatically persists cookies across requests via a shared `CookieContainer`. Enables testing session-based and authentication flows. Access via `.Cookies` property, reset with `.ClearCookies()`.
- **Swap.Testing: OOB Swap Introspection** — New methods on `HtmxTestResponse`: `GetOobSwapsAsync()` returns structured `OobSwap` records (TargetId, SwapMode, HtmlContent). `AssertOobSwapExistsAsync(targetId)`, `AssertOobSwapContentAsync(targetId, text)`, and `AssertOobSwapCountAsync(count)` for fluent OOB testing.
- **Swap.Testing: Trigger Payload Assertions** — `GetTriggerPayload<T>(eventName)` deserializes HX-Trigger JSON to typed objects. `AssertTriggerPayload(event, jsonPath, value)` asserts nested values via dot-path. `AssertTriggerCount(n)` verifies event count.
- **Swap.Testing: Form Field Helpers** — `AssertFormFieldExistsAsync(fieldName)` checks input/select/textarea presence. `AssertFormValueAsync(fieldName, value)` checks field values including checkboxes and selects.
- **Swap.Testing: Snapshot Scrubbers** — `SnapshotManager.ScrubUrls(pattern?)` replaces URLs with `[URL]`. `SnapshotManager.ScrubRegex(pattern, replacement)` for arbitrary pattern scrubbing.

### Fixed
- **`ClientAssetVersionDriftTests`** — Fixed test referencing `llms.md` instead of `llms.txt`.

---

## [1.1.1] - 2026-01-13

### Added
- **SwapStories (Component Playground)** - A new built-in tool for developing and testing Razor partials in isolation.
  - `[SwapStory]` attribute to mark stories.
  - `app.UseSwapStories()` middleware to serve detailed dashboard at `/_swap/stories`.
  - Auto-discovery of stories, viewport testing, and category grouping.
- **SwapErrorBoundaries** - Graceful error handling for HTMX requests. Intercepts exceptions and returns a customizable OOB error toast instead of crashing the UI with a full HTML page.
- **Secure SwapState** - Opt-in tamper-proof state using `IDataProtection` encryption for hidden fields and URL parameters.
- **`[SwapProtected]` / `[SwapUnprotected]`** - Attributes for fine-grained per-property protection control.
- **`@Html.SwapStateQueryString()`** - Helper to generate secure, encrypted query strings for `hx-get` links.

### Changed
- **Fixed Filter Clearing Bug** - Fixed an issue where clearing a filter input would restore the previous value from the hidden field (URL parameters now correctly take precedence over hidden fields).


## [1.1.0] - 2025-12-19

### Added
- New packages: `Swap.Htmx.Realtime` and `Swap.Htmx.Realtime.Redis`.
- `Swap.Htmx.Generators` version bumped to `1.1.0` (version alignment with `Swap.Htmx` v1.1.0).

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
