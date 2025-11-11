# Swap Framework Roadmap

## Current State (v0.4.2 - November 2025)

### What We Have
- **Swap.Htmx** (v0.3.3): HTMX framework with automatic view detection, simplified event system, and toast notifications
- **Swap.Modularity**: Module system for organizing features with automatic discovery
- **Swap.Testing**: HTMX-aware testing utilities with fluent assertions
- **Templates**: Four production-ready templates (minimal, monolith, layered, modular-monolith)
- **Swap.CLI** (v0.4.2): Project scaffolding tool with event system dev tools
- **Zero NPM Architecture**: All templates use Bulma CSS via LibMan (no build step required)

### What We Removed (Good Riddance)
- ❌ Swap.Patterns (code generation anti-pattern)
- ❌ Complex event resolution modes (Bidirectional, Transitive)
- ❌ Client-side subscription scanning (premature optimization)
- ❌ Server-side event filtering (unnecessary complexity)
- ❌ NPM/Tailwind/DaisyUI dependencies (simplified to Bulma via LibMan)

### Recent Accomplishments (v0.4.2)
- ✅ **Template Modernization**: Migrated all templates from Tailwind/DaisyUI/NPM to Bulma CSS via LibMan
- ✅ **Zero Build Complexity**: Removed all NPM infrastructure, CSS build steps, and node_modules
- ✅ **Simplified Development**: Single `libman restore` command for frontend dependencies
- ✅ **Documentation Overhaul**: Redesigned main README, updated all template READMEs
- ✅ **Minimal Template**: Created truly minimal starter template (swap-minimal)
- ✅ **Production Readiness**: Docker builds simplified (no Node.js stages), all templates tested

### Current Architecture Philosophy
**Minimal, predictable, HTMX-native, zero ceremony**. We do a few things exceptionally well rather than many things poorly.

---

## Immediate Priorities (Next 2-4 Weeks)

### 1. HTMX WebSocket & SSE Support 🔥 **TOP PRIORITY**
**Status**: SSE complete ✅, WebSockets incomplete ⚠️

**Completed:**
- [x] **Server-Sent Events (SSE)**: Native SSE support for server push ✅
  - `ServerSentEvents()` method in SwapController
  - `RenderPartialToStringAsync()` for rendering views to strings
  - `ServerSentEventStream` for sending events
  - Graceful connection closure with `sse-close` event
  - 6 unit tests (all passing)
  - 4 E2E tests (all passing)
  - Working demo at `/test/sse`
  - Complete documentation in `Swap.Htmx/Docs/SERVER-SENT-EVENTS.md`
  - Wiki documentation added

- [ ] **WebSocket Extension Integration**: HTMX WebSocket support (Incomplete) ⚠️
  - Server-side WebSocket infrastructure (`SwapWebSocketHandler`, `WebSocketConnection`) in place
  - Handler lifecycle wired (OnConnected, OnMessage, OnDisconnected)
  - Razor partial rendering support (`RenderPartialToStringAsync`) implemented
  - Middleware for mapping WebSocket handlers implemented
  - 8 unit tests passing
  - Chat demo rendering via OOB swaps pending final fix (messages not visible yet)
  - 6 E2E tests written but currently blocked by infra (running app conflict)
  - Documentation drafted in `Swap.Htmx/Docs/WEBSOCKETS.md` (finalize after UI fix)

**Remaining:**
- [ ] **Real-time Template Examples**: Add real-time features to templates
  - Live notifications in Demo module
  - Real-time todo updates
  - Connection status indicators
- [ ] **Wiki Documentation**: Add WebSocket docs to wiki after implementation is complete (SSE docs are live)

**Key Insight from Implementation:**
WebSocket messages in HTMX **must** use `hx-swap-oob` attribute to specify where content should be inserted. The ws extension uses Out-of-Band swap logic, not direct innerHTML replacement. This is critical for proper integration.

**Why this is top priority:**
- Completes HTMX feature coverage (WebSocket/SSE are major HTMX 2.x features)
- Real-time is increasingly expected in modern apps
- Differentiates Swap from basic MVC frameworks
- Relatively small scope, high impact

---

### 2. Testing Framework Enhancements
**Status**: Basic but functional

**Critical improvements:**
- [ ] **Snapshot Testing**: Compare full HTML responses for regression detection
  - Anti-forgery token scrubbing
  - GUID/timestamp normalization
  - Configurable scrubbers
- [ ] **Event Assertions**: Verify event emission in tests
  - `bus.Should().HaveEmitted("product.created").WithPayload(...)`
  - Chain execution verification
- [ ] **Better Fixtures**: Pre-configured factories for common test scenarios

---

## Short Term (Q1 2026)

### 3. Developer Experience Polish
**Status**: Functional but rough edges

**Critical improvements:**
- [ ] **Dev Tools Enhancements**:
  - Real-time chain visualization (WebSocket updates when chains change in dev)
  - Event history viewer (see what events fired during last N requests)
  - Chain recommendations (detect domain events without UI chains)
- [ ] **Better Error Messages**: When SwapView() is used incorrectly, provide helpful guidance
- [ ] **Analyzer Improvements**: Detect common mistakes
  - Emitting UI events instead of domain events
  - Missing chains for domain events
  - Invalid event name formats

---

## Short Term (Q1 2026)

### 3. Developer Experience Polish
**Status**: Functional but rough edges

**Critical improvements:**
- [ ] **Dev Tools Enhancements**:
  - Real-time chain visualization (WebSocket updates when chains change in dev)
  - Event history viewer (see what events fired during last N requests)
  - Chain recommendations (detect domain events without UI chains)
- [ ] **Better Error Messages**: When SwapView() is used incorrectly, provide helpful guidance
- [ ] **Analyzer Improvements**: Detect common mistakes
  - Emitting UI events instead of domain events
  - Missing chains for domain events
  - Invalid event name formats

---

## Medium Term (Q2-Q3 2026)

### 4. Swap.Modularity Evolution
**Status**: Works well, consider enhancements

**Potential improvements:**
- [ ] **Module Communication via Events**: Modules emit/listen to domain events (not direct dependencies)
- [ ] **Module Health Checks**: Each module reports health status
- [ ] **Module Hot Reload**: Dev mode only, reload module assemblies without restart (developer experience)
- [ ] **Module Packaging**: Modules as NuGet packages with proper versioning

**What we won't do:**
- ❌ Plugin architecture with dynamic loading (complexity explosion)
- ❌ Module marketplace (focus on framework, not ecosystem)

---

### 5. Performance & Production Readiness
**Status**: Templates are production-ready, need framework metrics

**Critical improvements:**
- [ ] **Benchmarks**: Establish baseline performance metrics
  - Request/response overhead (< 1ms target)
  - Event chain resolution (< 1ms for 1000 chains)
  - Memory footprint (< 10MB for event system)
- [ ] **Load Testing**: Simulate 1000 concurrent users, identify bottlenecks
- [ ] **Caching Strategies**: Document how to cache partial views effectively

**What we won't do:**
- ❌ Premature optimization (measure first)
- ❌ Custom caching implementation (use built-in ASP.NET features)

---

## Long Term (Q4 2026+)

### 6. Event System Polish
**Status**: Foundation solid, room for refinement

**Potential improvements:**
- [ ] **Event Payload Serialization Options**: Customize JSON serialization per event type
- [ ] **Event Validation Helpers**: Validate event name formats, detect chain cycles
- [ ] **Performance Optimization**: Measure and optimize event resolution overhead

**What we won't do:**
- ❌ Add filtering back (HTMX handles it client-side)
- ❌ Add complex resolution modes (one-hop is sufficient)
- ❌ Add event sourcing/replay (out of scope)

---

## Completed & Won't Do

### ✅ Decisions Made & Implemented

#### ✅ Out-of-Band (OOB) Swaps - **IMPLEMENTED**
SwapController supports OOB swaps. Can return multiple fragments in one response.

#### ✅ Folder Structure Enforcement - **NO (Decided)**
Templates show best practices. No runtime enforcement. Trust developers.

#### ✅ Module Middleware Contribution - **YES (Implemented)**
Modules can contribute middleware. App-level Program.cs controls ordering explicitly.

#### ✅ Multiple Event Buses - **NO (Decided)**
Single global event bus. Enforce namespacing via EventKey naming conventions.

#### ✅ Minimal Template - **IMPLEMENTED (v0.4.2)**
Created `swap-minimal` template. Truly minimal starter (< 10 files).

#### ✅ Zero NPM Architecture - **IMPLEMENTED (v0.4.2)**
All templates migrated to Bulma CSS via LibMan. No build step required. Simplified development workflow.

---

## Non-Goals (永久に)

Things we will **never** do, to keep the framework focused:

### 1. Code Generation
**Why not**: Generates coupling, creates maintenance burden, makes code harder to understand. We tried it. It was terrible.

### 2. Magic Configuration
**Why not**: Explicit is better than implicit. Convention-over-configuration leads to "how does this even work?" moments.

### 3. Full-Stack JavaScript Integration
**Why not**: We're a server-rendered framework. Use Alpine.js or vanilla JS for client-side behavior, but keep it simple.

### 4. Database Abstraction
**Why not**: EF Core exists. Use it. Don't reinvent wheels.

### 5. Microservices Support
**Why not**: Modular monolith is sufficient for 99% of apps. If you need microservices, use proper service mesh tools.

### 6. GraphQL/gRPC Integration
**Why not**: Out of scope. These are API technologies. We're HTMX/HTML.

### 7. Mobile App Support
**Why not**: HTMX is for web. Use proper mobile frameworks for mobile.

### 8. CSS Framework Lock-in
**Why not**: Bulma works great now, but we don't want to be tied to it forever. Templates show patterns, not frameworks.

### 9. Blocks/Components/Plugins
**Why not**: We're building a framework, not a plugin ecosystem. Third parties can build on top of Swap.

### 10. Alternative Rendering Engines
**Why not**: MVC/Razor is our focus. Razor Pages, Minimal APIs, Blazor hybrid would fragment effort and dilute quality.

---

## Summary: What's Next?

### 🔥 **Immediate Focus (Next 2-4 Weeks)**
1. **WebSocket & SSE Support** - Top priority to complete HTMX feature coverage
2. **Testing Enhancements** - Snapshot testing, event assertions

### 📅 **Q1 2026**
3. **Developer Experience** - Better dev tools, error messages, analyzers (module hot reload)

### 📅 **Q2-Q3 2026**
4. **Modularity Evolution** - Module events, health checks, packaging
5. **Performance & Production** - Benchmarks, load testing, caching strategies

### 📅 **Q4 2026+**
6. **Event System Polish** - Serialization options, validation helpers, optimization

---

## Decision Framework

When evaluating new features, ask:

### 1. **Does it make the happy path happier?**
   - If it requires complex configuration or "opt-in", it's probably not core.
   - If it's only useful in edge cases, it's probably not core.

### 2. **Does it reduce cognitive load?**
   - If developers need to learn new concepts, the benefit must be massive.
   - If it's "just another way" to do something, it's bloat.

### 3. **Does it align with HTMX philosophy?**
   - HTML over JSON
   - Server-rendered over client-rendered
   - Simple over clever

### 4. **Can we maintain it for 5+ years?**
   - If it requires constant updates as HTMX evolves, reconsider.
   - If it has complex dependencies, reconsider.

### 5. **Would we use it ourselves?**
   - If we wouldn't use it in a real project, don't build it.

---

## Metrics for Success

How do we know we're building the right thing?

### Framework Adoption
- **Target**: 100 GitHub stars by end of 2026 (quality over quantity)
- **Target**: 10 production apps using Swap (documented case studies)
- **Target**: 5 contributors beyond original author

### Developer Satisfaction
- **Target**: < 5 minutes from `dotnet new swap-monolith` to running app
- **Target**: < 30 minutes to build first feature (CRUD + events)
- **Target**: Zero "how do I..." questions that aren't in docs

### Framework Quality
- **Target**: 90%+ test coverage on core libraries
- **Target**: < 1% regression rate across releases
- **Target**: All issues responded to within 48 hours

### Performance
- **Target**: < 1ms overhead per request for event system
- **Target**: < 10MB memory for typical app
- **Target**: Handles 10k req/sec on modest hardware (4 core, 8GB RAM)

---

## Conclusion

**The goal isn't to build the biggest framework. It's to build the simplest framework that solves real problems.**

Every feature must earn its place. When in doubt, leave it out.

We're building a foundation, not a cathedral. Stay minimal. Stay focused. Stay critical.

**Next up: WebSocket & SSE support to complete HTMX feature coverage. Then polish, document, and grow the ecosystem thoughtfully.**
