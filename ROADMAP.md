# Swap.Htmx Roadmap: The Orchestration Era

**Current Version:** v0.5.1  
**Target:** v0.9.0  
**Philosophy:** Orchestrating reactive user interfaces in ASP.NET Core with the simplicity of HTMX.

---

## Vision Statement

Swap.Htmx is not just a set of helpers; it is an **orchestration layer** for your UI. It decouples **Events** (User Actions) from **Reactions** (UI Updates).

**The v0.9.0 Goal:** A library that handles the "messy middle" of modern web apps—validation, partial updates, and real-time events—without forcing you into a complex SPA framework.

---

## Phase 1: The Foundation (Critical Architecture)
**Timeline:** Immediate (1-2 months)  
**Focus:** Fixing "Hidden Dragons" that prevent production usage.

### 🟢 1.1 Async Everywhere (The "Sync-Over-Async" Fix)
**Priority:** Critical (Blocker)  
**Status:** ✅ Completed
**Problem:** Current event chains use synchronous delegates (`Func<HttpContext, object>`). You cannot query a database to get data for a partial view without risking thread pool starvation.
**Solution:**
- [x] Update `EventChainExecutor` to support `Func<HttpContext, Task<object?>>`.
- [x] Add `RefreshPartialAsync` and `RefreshPartial` (async overloads).
- [x] Ensure all internal pipelines are async-first.

### 🟢 1.2 Composition Over Inheritance
**Priority:** Critical (Adoption Blocker)  
**Status:** ✅ Completed
**Problem:** `SwapController` is a "God Object". It forces inheritance, conflicts with other base classes, and hides dependencies.
**Solution:**
- [x] **Deprecate** `SwapController` as a requirement.
- [x] **Introduce** `ISwapEventService` for dependency injection.
- [x] **Introduce** `ControllerBase` extension methods (`this.SwapResponse()`, `this.SwapEvent()`).
- [x] **Technical Challenge:** Resolved `ICompositeViewEngine` and `IModelMetadataProvider` via `HttpContext.RequestServices` inside the service.

### 🟢 1.3 First-Class Form Validation
**Priority:** High (DX Pain Point)  
**Status:** ✅ Completed
**Problem:** Handling `ModelState` errors in HTMX is currently manual and verbose. It is the most common task in CRUD apps.
**Solution:**
- [x] Add `SwapValidationErrors` extension.
- [x] Automatic `validationFailed` trigger and warning toast.
- **API Preview:**
  ```csharp
  if (!ModelState.IsValid) 
  {
      return this.SwapValidationErrors(ModelState)
                 .AlsoUpdate("form-id", "_FormPartial", model)
                 .Build();
  }
  ```

---

## Phase 2: Scaling & Architecture (The "MediatR" Shift)
**Timeline:** Months 2-4  
**Focus:** Solving "Action at a Distance" and organizing code for larger apps.

### 🟢 2.1 Decentralized Configuration
**Priority:** High  
**Status:** ✅ Completed
**Problem:** Configuring all events in `Program.cs` violates "Locality of Behavior" and creates a massive file.
**Solution:**
- [x] Introduce `ISwapEventConfiguration` interface.
- [x] Allow registering feature-specific configs via `options.AddConfig<T>()`.
- **API Preview:**
  ```csharp
  // In Program.cs
  builder.Services.AddSwapHtmx(options => {
      options.AddConfig<CartEventConfig>();
  });
  
  // In Features/Cart/CartEventConfig.cs
  public class CartEventConfig : ISwapEventConfiguration {
      public void Configure(SwapEventBusOptions events) {
          events.When(CartEvents.Updated).RefreshPartial(...);
      }
  }
  ```

### 🟢 2.2 Bulk & List Operations
**Priority:** Medium  
**Status:** ✅ Completed
**Problem:** Updating a list of items (e.g., "Mark all as read") requires manual loop/ID generation.
**Solution:**
- [x] Add `AlsoUpdateMany` helper.
- **API Preview:**
  ```csharp
  return SwapResponse()
      .AlsoUpdateMany(items, x => $"task-{x.Id}", "_TaskRow");
  ```

---

## Phase 3: Modern .NET & Platform Expansion
**Timeline:** Months 4-6  
**Focus:** Embracing the future of .NET.

### 🟢 3.1 Minimal APIs Support
**Priority:** Medium/High  
**Status:** ✅ Completed
**Problem:** Minimal APIs are the default, but Swap relies on Controller context for View rendering.
**Solution:**
- [x] Create `SwapResult` implementation of `IResult`.
- [x] Create `SwapResults` static factory for Minimal API endpoints.
- [x] Ensure `ICompositeViewEngine` works via DI in Minimal API context.
- **API Preview:**
  ```csharp
  app.MapGet("/hello", () => SwapResults.Response().WithView("Hello"));
  ```

### 🟢 3.2 Razor Pages Support
**Priority:** Medium
**Status:** ✅ Completed
**Problem:** Many enterprise apps use Razor Pages.
**Solution:**
- [x] Add `PageModel` extensions (`this.SwapResponse()`, `this.SwapEvent()`).
- [x] Create `SwapPageResult` for handling Razor Page rendering.
- [x] Update `SwapResponseBuilder` to support `PageModel` context.

---

## Phase 4: Enterprise Polish
**Timeline:** Months 6+
**Focus:** Security, Observability, and Scalability hooks.

### 🔒 4.1 Authorization & Security
**Priority:** High
**Problem:** SSE broadcasts currently go to everyone or generic rooms.
**Solution:**
- [ ] Capture `ClaimsPrincipal` on SSE connection.
- [ ] Add `ToUser(userId)` and `ToRole(roleName)` to event chains.
- [ ] Secure the SSE endpoint against unauthorized access.

### 📊 4.2 Observability (OpenTelemetry)
**Priority:** Medium
**Problem:** Debugging complex event chains in production is blind.
**Solution:**
- [ ] Implement `ActivitySource` for OpenTelemetry tracing.
- [ ] Trace Event Chain execution, OOB rendering, and SSE broadcasts.
- [ ] Add standard metrics (e.g., `swap_events_triggered_total`).

### 🧩 4.3 Distributed Scalability Abstractions
**Priority:** High (Architectural)
**Status:** ✅ Completed
**Problem:** The current in-memory SSE implementation cannot scale to web farms.
**Solution:**
- [x] Extract `ISseBackplane` interface.
- [x] Ensure the core library is "Cluster-Ready" (even if the default implementation is in-memory).
- [x] *Note: This enables future Redis/NATS implementations.*

---

## Deprioritized / Dropped (YAGNI)
*   **Wizards:** Too niche. Can be built with standard partials.
*   **Heavy Telemetry:** Basic logging/OpenTelemetry is enough.
*   **Blazor Hybrid:** Too complex for v0.9.0. Focus on being the best *HTMX* library first.

---

## Success Criteria for v0.9.0
1.  **Async-First:** No synchronous DB access in event chains.
2.  **No Inheritance:** `SwapController` is gone (or optional).
3.  **Validation:** `ModelState` handling is one line of code.
4.  **Organized:** Events can be configured per-feature, not just globally.
5.  **Secure:** SSE broadcasts respect user permissions.

This roadmap positions Swap.Htmx not just as a helper library, but as a **fundamental architectural pattern** for building maintainable, server-driven .NET applications.