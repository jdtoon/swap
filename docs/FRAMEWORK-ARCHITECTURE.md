# Swap Framework Architecture

**Last Updated**: November 3, 2025  
**Status**: Updated for v0.3.0 (event system shipped)  
**Purpose**: Define the comprehensive framework structure and event system

---

## 📖 Document Navigation

**This Document**: Framework overview, package structure, and high-level architecture

**Related Documents**:
- **[EVENT-SYSTEM-DESIGN.md](EVENT-SYSTEM-DESIGN.md)** - Complete event system specification with implementation details
- **[PRODUCT-VISION.md](PRODUCT-VISION.md)** - Overall product direction and three core pillars

---

## 🎯 Framework Philosophy

**Swap is not a library—it's a framework with rigidity and structure.**

While developers can customize after generation, Swap provides **opinionated, battle-tested patterns** that guide best practices. The framework layer (`Swap.*` packages) extends .NET to be **HTMX-native**, providing out-of-the-box C# code that developers can plug and play.

### Core Principles

1. **Framework Over Library** - We dictate structure for consistency and maintainability
2. **HTMX-Native .NET** - Extend ASP.NET Core specifically for HTMX workflows
3. **Battle-Tested Only** - Every feature comes from production usage
4. **Security First** - Components include security considerations by default
5. **Plug & Play** - Easy setup, clear wiring, instant productivity

---

## 📦 Framework Packages

### Current State (v0.3.0)

```
framework/
├── Swap.Htmx/           ✅ Core HTMX integration + Event System (v0.3.0)
├── Swap.Patterns/       ✅ Domain patterns (v0.3.0)
├── Swap.Testing/        ✅ HTMX testing utilities (v0.3.0)

templates/
└── components/          🆕 UI component templates (installed as source)
```

### Planned Expansion

```
framework/
├── Swap.Htmx/           ⭐ Ongoing enhancements (dev tooling, DX)
├── Swap.Auth/           🆕 Authentication & authorization
├── Swap.WebSockets/     🆕 Real-time updates via WebSockets
├── Swap.SignalR/        🆕 SignalR integration for HTMX
├── Swap.Validation/     🆕 Server-side validation patterns
├── Swap.Forms/          🆕 Form handling and generation
└── Swap.Api/            🆕 REST API helpers (secondary to HTMX)

templates/
└── components/          🆕 Component templates (copied into your app)
```

---

## 🏗️ Swap.Htmx - The Foundation

**Purpose:** Make ASP.NET Core HTMX-native with zero friction.

### Current Features (v0.3.0)

```csharp
// 1. SwapController - Automatic page/partial detection
public class ProductsController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var products = await _service.GetAllAsync();
        return SwapView(products); // Returns full page OR partial automatically
    }
}

// 2. Request Detection
if (Request.IsHtmxRequest()) { }
if (Request.IsHtmxBoosted()) { }
var target = Request.GetHtmxTarget();
var trigger = Request.GetHtmxTrigger();

// 3. Response Headers
Response.HxTrigger("productCreated");
Response.HxRedirect("/products");
Response.HxRefresh();
Response.HxRetarget("#product-list");
Response.HxReswap("beforebegin");
Response.HxPushUrl($"/products/{id}");

// 4. Toast Notifications
Response.ShowSuccessToast("Product created!");
Response.ShowErrorToast("Validation failed");
Response.ShowWarningToast("Stock low");
Response.ShowInfoToast("Processing...");

// 5. Event System (Server-driven + filtered)
// Configure in Program.cs
// builder.Services.AddSwapHtmx(events => {
//   events.Chain("todo.created", "ui.todo.refreshList");
//   events.ResolutionMode = ChainResolutionMode.OneHop; // or Bidirectional/Transitive
// });
// app.UseSwapHtmx();
// if (app.Environment.IsDevelopment()) app.MapSwapHtmxDevEndpoints();

// Emit in controller
await _events.EmitAsync("todo.created", new { id = 123 });

// 6. Middleware
app.UseSwapHtmxShell(); // Enforces partial responses for HX-Request
```

### Planned Enhancements

#### 1. WebSocket Integration

**Purpose:** Real-time updates for collaborative features, notifications, live dashboards.

```csharp
// Server-side
public class NotificationsHub : SwapHub
{
    public async Task SendNotification(string userId, string message)
    {
        // Automatically serializes to HTMX-compatible format
        await Clients.User(userId).SendHtmlAsync("notification-container", 
            await RenderPartialAsync("_Notification", message));
    }
}

// Client-side (HTMX + WebSocket extension)
<div hx-ext="ws" ws-connect="/notificationHub">
    <div id="notification-container"></div>
</div>

// Controller can trigger WebSocket updates
public async Task<IActionResult> CreateOrder(Order order)
{
    await _service.CreateAsync(order);
    
    // Trigger WebSocket update to all connected clients
    await _hub.Clients.All.SendHtmlAsync("order-list", 
        await RenderPartialAsync("_OrderList"));
    
    return SwapView("Details", order);
}
```

**Implementation:**
- `Swap.WebSockets` package with `SwapHub` base class
- Integration with HTMX WebSocket extension
- Automatic HTML partial rendering via SignalR/WebSockets
- Connection management and reconnection logic

#### 2. Server-Sent Events (SSE)

**Purpose:** One-way real-time updates (progress bars, live feeds, notifications).

```csharp
// Controller
public async IAsyncEnumerable<string> ProcessOrders()
{
    await foreach (var result in _service.ProcessOrdersAsync())
    {
        // Yield HTML partials that HTMX will swap in
        yield return await RenderPartialAsync("_ProcessingStatus", result);
    }
}

// View
<div hx-ext="sse" sse-connect="/orders/process" sse-swap="processingStatus">
    <div id="status">Waiting...</div>
</div>
```

**Implementation:**
- Extension methods for SSE responses
- Automatic partial rendering
- Progress tracking utilities

#### 3. Long Polling Helper

**Purpose:** Fallback for WebSocket/SSE, polling with exponential backoff.

```csharp
public class JobsController : SwapController
{
    [HtmxLongPoll(intervalMs: 1000, maxAttempts: 30)]
    public async Task<IActionResult> CheckJobStatus(string jobId)
    {
        var job = await _service.GetJobAsync(jobId);
        
        if (job.IsComplete)
        {
            Response.HxStopPolling(); // Custom header to stop polling
            return SwapView("_JobComplete", job);
        }
        
        return SwapView("_JobProgress", job);
    }
}

// View - automatically polls with smart backoff
<div hx-get="/jobs/status?jobId=123" 
     hx-trigger="load" 
     hx-swap="outerHTML">
</div>
```

**Implementation:**
- `[HtmxLongPoll]` attribute
- Exponential backoff strategy
- `HxStopPolling()` response header
- Client-side JavaScript for polling logic

---

## ⚡ Event System - The Critical Component

**This is where Swap will shine.** The event system must be comprehensive, battle-tested, and incredibly easy to use.

> **📘 Full Design Specification:** See [EVENT-SYSTEM-DESIGN.md](EVENT-SYSTEM-DESIGN.md) for complete implementation details.

### Core Concept: Intelligent Event Filtering

The Swap event system uses a **client-side event registry** that tells the server which events are active on the current page. The server only sends events that have active listeners, eliminating wasted bandwidth and improving performance.

**Key Features:**
- ✅ **Event Registry** - Components declare event subscriptions in markup
- ✅ **Automatic Filtering** - Server only sends events with active listeners
- ✅ **Event Chains** - Define event workflows once, use everywhere
- ✅ **Zero Waste** - No unnecessary event triggers
- ✅ **Type-Safe** - Centralized event naming convention
- ✅ **Debuggable** - Built-in debugging tools

### Architecture Overview

```
Browser                          Server
├── Event Registry              ├── Event Context Middleware
│   (sessionStorage)            │   (Extracts active events)
├── Component Registration      ├── Event Chain Resolver
│   (data-swap-events)          │   (Resolves dependencies)
└── HTMX Interceptor            └── Event Filter & Response
    (X-Swap-Events header)          (Builds HX-Trigger)
```

### Current State (Basic Events)

```csharp
// Server triggers event
Response.HxTrigger("productCreated");

// Client listens
<div hx-get="/products" hx-trigger="productCreated from:body">
```

**Problems with Basic Approach:**
- ❌ No standardized event naming convention
- ❌ No event payload validation
- ❌ No event chaining/pipeline
- ❌ No event bus for complex workflows
- ❌ Hard to track event flow in complex apps
- ❌ No debugging tools for events
- ❌ Manual wiring for every event listener
- ❌ Server doesn't know what's on the page (sends events to nothing)

### Event System Solution

The complete event system design addresses all these problems through:

1. **Client-Side Event Registry** - Components declare subscriptions, browser tracks active events
2. **Server-Side Filtering** - Only emit events with active listeners on current page
3. **Event Chains** - Define workflows once, reuse everywhere
4. **Standard Events** - Type-safe event naming convention
5. **Debugging Tools** - Built-in event inspector and logging

**Quick Example:**

```csharp
// 1. Component declares events in markup
<div data-swap-component="product-list"
     data-swap-events="product.created,product.updated,product.deleted">
</div>

// 2. Browser sends active events with each request
// X-Swap-Events: product.created,product.updated,product.deleted

// 3. Server emits event
await _eventBus.EmitAsync(SwapEvents.Entity.Created("product"));

// 4. Server resolves chains and filters to active events only
// product.created → ui.refreshList, stats.updated (both active on page)

// 5. Browser receives filtered events
// HX-Trigger: {"product.created": {"id": 123}, "ui.refreshList": null}
```

**For complete implementation details, architecture diagrams, code examples, and API reference, see:**

> **📘 [EVENT-SYSTEM-DESIGN.md](EVENT-SYSTEM-DESIGN.md)** - Complete event system specification

### Event System Quick Reference

**Standard Event Naming:**
```csharp
public static class SwapEvents
{
    public static class Entity
    {
        public static string Created(string name) => $"{name}.created";
        public static string Updated(string name) => $"{name}.updated";
        public static string Deleted(string name) => $"{name}.deleted";
    }
    
    public static class UI
    {
        public const string RefreshList = "ui.refreshList";
        public const string OpenModal = "ui.openModal";
        public const string CloseModal = "ui.closeModal";
        public const string ShowToast = "ui.showToast";
    }
    
    public static class Auth
    {
        public const string LoggedIn = "auth.loggedIn";
        public const string LoggedOut = "auth.loggedOut";
        public const string SessionExpired = "auth.sessionExpired";
    }
}
```

**Event Bus API (v0.3.0):**
```csharp
public interface ISwapEventBus
{
    Task EmitAsync(string eventName, object? payload = null, CancellationToken ct = default);
    void Emit(string eventName, object? payload = null);
    void ClearPendingEvents();
}
```

**Configuration:**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.Chain("product.created", "ui.refreshList", "stats.updated");
    events.ResolutionMode = ChainResolutionMode.OneHop; // or Bidirectional/Transitive
    events.MaxTransitiveDepth = 2; // only for Transitive
});

app.UseSwapHtmx();
if (app.Environment.IsDevelopment())
{
    app.MapSwapHtmxDevEndpoints();
}
```

**Controller Usage:**
```csharp
public async Task<IActionResult> Create(ProductDto dto, [FromServices] ISwapEventBus events)
{
    var product = await _service.CreateAsync(dto);
    
    // Emit event - server resolves chains and filters to active listeners
    await events.EmitAsync("product.created", new { id = product.Id });
    
    return SwapView("Details", product);
}
```

**Component Declaration:**
```html
<div data-swap-component="product-list"
     data-swap-events="product.created,product.updated,product.deleted"
     hx-get="/products"
     hx-trigger="load, product.created from:body, product.updated from:body">
</div>
```

**For full implementation details:**
- Client-side `SwapEventRegistry` class
- Server-side `SwapEventBus` implementation
- Event chain resolution algorithm
- Filtering logic
- Standard event patterns
- Debugging tools
- Performance considerations
- Complete code examples

> **📘 See [EVENT-SYSTEM-DESIGN.md](EVENT-SYSTEM-DESIGN.md)**

---

## 🧩 Component templates (you own the code)

**Purpose:** Provide battle-tested UI patterns as templates that are copied into your app as source code. You customize them freely; no framework package required.

### Where templates live

```
templates/components/
├── table/
├── modal/
├── pagination/
├── search-bar/
└── toast/
```

When installed, the CLI copies files into your project (for example):

```
YourApp/
├── Views/Components/Table/        # .cshtml partials/partials
├── Views/Shared/_Toast.cshtml
├── wwwroot/css/components.css     # optional assets
└── Controllers/Partials/          # optional controller endpoints if needed
```

### Installing templates

```bash
# Install a single component template (copies code into your app)
swap add component table

# Install a category of templates
swap add components forms

# Preview before installing
swap component preview table
```

**What happens:**
1. Template files are copied into your project (you own and edit them)
2. Optional wiring is applied (routes, Program.cs registration, assets)
3. No NuGet dependency for UI; updates are opt-in by re-running or merging templates

### Using templates

```csharp
// Controller
public class ProductsController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var data = await _service.GetAllAsync();
        return SwapView(data);
    }
}
```

```html
<!-- View -->
@model IEnumerable<Product>

<partial name="_PageHeader" model="new { Title = \"Products\" }" />
<partial name="_SearchBar" model="new SearchBarModel { HxGet = Url.Action(\"Index\"), HxTarget = \"#product-list\" }" />

<div id="product-list">
  <partial name="_Table" model="Model" />
  <partial name="_Pagination" model="ModelPagination" />
  <partial name="_Toast" />
  <!-- Templates include data-swap-component/data-swap-events where appropriate -->
  <!-- so they participate in the event system automatically. -->
  </div>
```

Templates follow the event system conventions (data-swap-component, data-swap-events, hx-trigger) so they register with the client event registry and respond to server-emitted events out of the box.

---

## 🔐 Swap.Auth - Authentication & Authorization

**Purpose:** Battle-tested auth patterns with HTMX integration.

### Features

```csharp
// 1. HTMX-aware authentication
[HtmxAuthorize] // Returns 401 partial instead of redirect
public class AdminController : SwapController
{
    public IActionResult Dashboard() => SwapView();
}

// 2. Role-based access
[HtmxAuthorize(Roles = "Admin,Manager")]
public IActionResult Settings() => SwapView();

// 3. Claims-based
[HtmxRequireClaim("Permission", "Products.Edit")]
public IActionResult Edit(int id) => SwapView();

// 4. HTMX-friendly login
public class AuthController : SwapController
{
    [HttpPost]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password);
        
        if (result.Succeeded)
        {
            Response.HxRedirect("/dashboard");
            await _eventBus.EmitAsync(SwapEvents.Auth.LoggedIn);
            return Ok();
        }
        
        Response.HxRetarget("#login-form");
        return PartialView("_LoginForm", dto);
    }
}

// 5. Session timeout handling
app.UseSwapSessionTimeout(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.OnTimeout = (context) =>
    {
        if (context.Request.IsHtmxRequest())
        {
            context.Response.HxTrigger(SwapEvents.Auth.SessionExpired);
            context.Response.HxRedirect("/login");
        }
    };
});
```

---

## 🎨 Swap.Validation - Server-Side Validation

**Purpose:** HTMX-native validation with smart error retargeting.

```csharp
// Automatic validation error handling
[HttpPost]
[ValidateModel] // Custom attribute
public async Task<IActionResult> Create(ProductDto dto, [FromServices] ISwapEventBus events)
{
    // If invalid, automatically:
    // 1. Retargets to form container
    // 2. Returns validation errors in HTMX format
    // 3. Triggers error toast
    // No manual ModelState checking needed!
    
    var product = await _service.CreateAsync(dto);
    await events.EmitAsync("product.created", new { id = product.Id });
    Response.ShowSuccessToast("Created");
    return SwapView("Details", product);
}

// Custom validation rules
public class ProductDtoValidator : SwapValidator<ProductDto>
{
    public ProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithHtmxError("#product-name-error"); // Targets specific error div
        
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithHtmxError("#product-price-error");
    }
}
```

---

## 🧪 Framework Testing Strategy

**Principle:** Every framework feature must be battle-tested before inclusion.

### Testing Levels

1. **Unit Tests** - Test individual classes/methods
2. **Integration Tests** - Test framework integration with ASP.NET Core
3. **Component Tests** - Test UI components in isolation
4. **E2E Tests** - Test complete workflows with HTMX interactions
5. **Real App Tests** - Build sample apps, extract patterns that work

### Test-Driven Framework Development

```csharp
// 1. Write test for desired feature
[Fact]
public async Task EventBus_Should_ResolveChains_And_Filter()
{
    // Arrange: configure chains
    var options = new SwapEventBusOptions();
    options.Chain("product.created", "ui.refreshList", "ui.showToast");
    var http = new DefaultHttpContext();
    // active subscriptions on this page
    http.Items[SwapEventKeys.ActiveEvents] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "product.created", "ui.refreshList", "ui.showToast"
    };
    var accessor = new HttpContextAccessor { HttpContext = http };
    var bus = new SwapEventBus(accessor, options);

    // Act
    await bus.EmitAsync("product.created", new { id = 1 });
    // middleware would build headers at end of pipeline
    var (resolved, _) = bus.ResolveAndFilterFor(http);

    // Assert
    Assert.True(resolved.ContainsKey("product.created"));
    Assert.True(resolved.ContainsKey("ui.refreshList"));
    Assert.True(resolved.ContainsKey("ui.showToast"));
}

// 2. Implement feature to pass test
// 3. Build sample app using feature
// 4. Extract learnings, improve API
// 5. Repeat
```

---

## 🚀 Implementation Roadmap

### Phase 1: Event System Foundation (Weeks 1-2)

**Goal:** Build core event bus with basic functionality.

> **📘 Full implementation specification: [EVENT-SYSTEM-DESIGN.md](EVENT-SYSTEM-DESIGN.md)**

**Tasks:**
1. Create `Swap.Events` namespace in `Swap.Htmx`
2. Implement `ISwapEventBus` interface
3. Implement `SwapEventBus` with basic emit/on
4. Create `SwapEvents` static registry
5. Add unit tests for event bus
6. Update documentation

**Deliverables:**
- ✅ Working event bus with emit/on
- ✅ Event naming convention enforced
- ✅ Unit test coverage >90%
- ✅ Basic documentation with examples

### Phase 2: Event Chains & Pipelines (Weeks 3-4)

**Goal:** Enable complex event workflows.

> **📘 See [EVENT-SYSTEM-DESIGN.md](EVENT-SYSTEM-DESIGN.md) - Event Chains & Filtering sections**

**Tasks:**
1. Implement `IEventChain` interface
2. Build chain builder fluent API
3. Add conditional events (`EmitIfAsync`)
4. Implement batch event emission
5. Create event middleware pipeline
6. Integration tests with sample app

**Deliverables:**
- ✅ Event chaining works end-to-end
- ✅ Middleware pipeline functional
- ✅ Sample app demonstrating chains
- ✅ Documentation with real-world examples

### Phase 3: Component Templates (Weeks 5-8)

**Goal:** Extract and ship reusable UI as templates copied into apps (no framework package).

**Tasks:**
1. Create `templates/components/` source tree
2. Extract Table/Modal/Toast/Pagination/Search templates from CRUD
3. Provide optional lightweight helpers (no base classes)
4. Create component installation CLI commands (copy templates)
5. Build 3 sample apps using the templates

**Deliverables:**
- ✅ 5+ reusable component templates
- ✅ Installation via CLI (copies source into app)
- ✅ Templates wired to event system conventions
- ✅ Documentation with preview screenshots

### Phase 4: WebSocket Integration (Weeks 9-12)

**Goal:** Real-time updates via WebSockets.

**Tasks:**
1. Create `Swap.WebSockets` project
2. Build `SwapHub` base class (SignalR wrapper)
3. Implement HTML partial sending via WebSocket
4. Create connection management utilities
5. Add HTMX WebSocket extension integration
6. Build chat app sample
7. Build live dashboard sample
8. Performance testing

**Deliverables:**
- ✅ WebSocket integration working
- ✅ 2 sample apps demonstrating usage
- ✅ Performance benchmarks
- ✅ Documentation with architecture diagrams

### Phase 5: Auth & Validation (Weeks 13-16)

**Goal:** Security-first auth and validation.

**Tasks:**
1. Create `Swap.Auth` project
2. Implement `[HtmxAuthorize]` attribute
3. Build session timeout handling
4. Create `Swap.Validation` project
5. Implement `[ValidateModel]` attribute
6. Build FluentValidation integration
7. Add HTMX-aware error retargeting
8. Security audit

**Deliverables:**
- ✅ Auth system with HTMX integration
- ✅ Validation system with smart retargeting
- ✅ Security best practices documented
- ✅ Sample apps with auth flows

### Phase 6: Polish & Documentation (Weeks 17-20)

**Goal:** Production-ready framework with excellent docs.

**Tasks:**
1. Build event debugger UI
2. Create component gallery website
3. Write comprehensive guides
4. Record video tutorials
5. Build more sample apps
6. Community feedback iteration
7. Performance optimization
8. Prepare v1.0 release

**Deliverables:**
- ✅ Production-ready v1.0
- ✅ Component gallery live
- ✅ Documentation site complete
- ✅ 5+ sample apps
- ✅ Framework launched

---

## 📋 Framework Consistency Guidelines

### Event Naming Convention

```
{domain}.{action}        # Entity events: product.created, user.updated
{component}.{action}     # UI events: modal.opened, form.submitted
{system}.{action}        # System events: auth.loggedIn, cache.cleared
```

### Component Naming Convention

```
{ComponentName}Component        # Class: TableComponent, ModalComponent
Components/{Name}/{View}.cshtml # View: Components/Table/Table.cshtml
swap add component {name}       # CLI: swap add component table
```

### Extension Method Convention

```csharp
// Request extensions: Is*, Get*
Request.IsHtmxRequest()
Request.GetHtmxTarget()

// Response extensions: Hx*, Show*, Trigger*
Response.HxTrigger("event")
Response.ShowSuccessToast("message")
Response.TriggerCreated(entity)

// Event bus: Emit*, On*
_eventBus.EmitAsync("event")
_eventBus.OnEvent("event", handler)
```

### Attribute Convention

```csharp
[HtmxAuthorize]         # HTMX-aware authorization
[ValidateModel]         # Automatic validation with retargeting
[ListensTo("event")]    # Component listens to event
[Component("name")]     # Register as component
```

---

## 🔍 Framework Design Principles

1. **Plug & Play**
   - Install package → Add one line to Program.cs → It works
   - No complex configuration required
   - Sensible defaults, easy overrides

2. **Progressive Enhancement**
   - Basic functionality works without JavaScript
   - HTMX adds interactivity
   - WebSockets add real-time (optional)

3. **Security by Default**
   - Authentication checks built-in
   - CSRF protection automatic
   - XSS prevention in components
   - SQL injection impossible (use EF Core)

4. **Performance First**
   - Minimize JavaScript bundle size
   - Server-rendered is fast
   - Caching strategies built-in
   - Database query optimization helpers

5. **Developer Experience**
   - Clear error messages
   - IntelliSense everywhere
   - Type-safe APIs
   - Comprehensive logging

6. **Battle-Tested**
   - Every feature from production usage
   - Security reviewed
   - Performance benchmarked
   - Community validated

---

## 📚 Documentation Strategy

### 1. Framework API Reference

Auto-generated from XML comments:
- Every public class documented
- Every public method documented
- Examples for complex features
- Published to docs.swap.dev

### 2. Component Gallery

Interactive website:
- Live preview of each component
- Props/parameters reference
- Copy-paste code snippets
- Composition examples
- Published to components.swap.dev

### 3. Architecture Guides

Conceptual documentation:
- How event system works
- Component composition patterns
- WebSocket integration guide
- Security best practices
- Performance optimization

### 4. Sample Applications

Real-world examples:
- E-commerce site (product catalog, cart, checkout)
- Project management (kanban boards, tasks, teams)
- CRM (contacts, deals, activities)
- Blog platform (posts, comments, tags)
- Each app demonstrates different framework features

### 5. Video Tutorials

YouTube series:
- Getting started (10 min)
- Building your first component (15 min)
- Event system deep dive (20 min)
- Real-time features with WebSockets (25 min)
- Production deployment (30 min)

---

## ✨ Success Criteria

**How we know the framework is successful:**

### Developer Adoption
- ⏱️ Time to "Hello World" < 5 minutes
- 📦 Component installation < 10 seconds
- 🎓 Junior developer productive < 1 day
- 💬 "This is so easy!" feedback

### Technical Excellence
- 🧪 Test coverage > 90%
- ⚡ Response time < 100ms (server-rendered)
- 🔐 Zero security vulnerabilities
- 📈 Performance benchmarks beat SPA frameworks

### Community Growth
- ⭐ GitHub stars > 1,000 in year 1
- 📦 NuGet downloads > 10,000/month
- 💬 Active Discord community
- 🤝 Contributors from community

### Production Usage
- 🚀 5+ production apps using Swap
- 📊 Framework handles 1M+ requests/day
- 🎯 Zero critical bugs in production
- 😊 Developers report shipping faster

---

## 🎯 Summary

**Swap Framework = HTMX-Native .NET**

**Core Components:**
1. ✅ `Swap.Htmx` - Foundation (request/response helpers, middleware)
2. ⚡ `Event System` - **CRITICAL** - Event bus, chains, pipelines
3. 🧩 Component templates - Reusable UI delivered as templates (source you own)
4. 🔌 `Swap.WebSockets` - Real-time updates
5. 🔐 `Swap.Auth` - Security-first authentication
6. ✅ `Swap.Validation` - Server-side validation

**Framework Philosophy:**
- Rigid structure with clear conventions
- Battle-tested patterns only
- Security and performance first
- Plug & play simplicity
- Comprehensive event system for component coordination

**Next Steps:**
1. Implement event system now (consolidated focus)
2. Provide component templates (install via CLI, copied into app)
3. Add WebSocket support (Phase 4)
4. Build auth/validation (Phase 5)
5. Polish and launch v1.0 (Phase 6)

**This framework will make .NET + HTMX development as productive as Rails, with better type safety, performance, and developer experience.**
