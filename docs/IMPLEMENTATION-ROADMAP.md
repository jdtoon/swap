# Swap Implementation Roadmap - Battle-Tested Increments

**Last Updated**: November 3, 2025  
**Current Version**: 0.3.0  
**Status**: Active Development  
**Focus**: Developer Experience First

---

## 🎯 The Vision in One Sentence

**Build a framework developers actually want to use by testing each feature in real applications before shipping it.**

---

## 🧭 Guiding Principles

### 1. **Developers Are The Product**
- Every feature must pass the "Would I use this?" test
- DX is not secondary—it's the entire point
- If it's not delightful, it doesn't ship

### 2. **Battle-Test Everything**
- Build a real app using the feature
- Extract patterns from usage, not theory
- Ship only what proves valuable

### 3. **Small Increments, Big Impact**
- Each release adds one powerful capability
- Features must be immediately useful
- No "foundation for future features" that sit unused

### 4. **Framework, But Tool First**
- CLI orchestrates everything
- Components are drop-in, not configuration nightmares
- Zero magic—developers own their code

---

## 📍 Where We Are (v0.3.0)

### ✅ What's Working

**Core Framework (`Swap.Htmx`):**
- `SwapController` base class with automatic partial/full page detection
- Request detection helpers (`IsHtmxRequest()`, `GetHtmxTarget()`, etc.)
- Response header helpers (`HxTrigger()`, `HxRedirect()`, `HxRefresh()`, etc.)
- Toast notification extensions (`ShowSuccessToast()`, etc.)
- Middleware for shell enforcement

**Event System (New in 0.3.0):**
- HTMX-native client filtering via `X-Swap-Events` header
- Centralized chains with validation (naming + cycle detection)
- Resolution modes: `OneHop` (default), `Bidirectional`, `Transitive` with depth
- Dev endpoints: `/ _swap/dev/events`, `/events.json`, `/events.meta.json`, `/explain.json`
- CLI: `events list`, `events from-server`, `events validate`, `events graph`
- Dev dashboard: chains table, Mermaid graph, and Explain tooling

**CLI (`swap`):**
- `swap new MyApp` - Create monolith project from template
- `swap generate model Product --fields Name:string,Price:decimal`
- `swap generate controller Product`
- `swap generate resource Product` - Full CRUD generation
- `swap db migrate`, `swap db update` - Database commands
- `swap doctor` - Environment checks
 - Event commands: `events list`, `events from-server`, `events validate`, `events graph`

**What Makes This Good:**
- ✅ Actually generates working code
- ✅ Uses proven patterns (from TTW, Kanban, Habits, Carestream)
- ✅ Saves hours of boilerplate

**What's Missing:**
- ❌ Components are embedded in CRUD templates, not reusable
- ❌ Event system per-root overrides (optional) – deferred
- ❌ Only one architecture template (monolith)
- ❌ CRUD generation dominates the framework identity

---

## 🎯 Where We're Going

### The Vision Shift

**From:**
> Swap is a CRUD generation tool that outputs HTMX code

**To:**
> Swap is a productivity framework built on:
> 1. **Event System** - THE foundation - intelligent, debuggable coordination
> 2. **Component Templates** - Drop-in UI code you own (installed via CLI)
> 3. **Architecture Templates** - Start with the right structure
> 4. **CLI Orchestration** - Wire everything together effortlessly

### Why This Matters

**Current State:** Developer runs `swap generate resource Product` and gets 500+ lines of code to maintain.

**Future State:** Developer runs `swap new MyApp`, adds components, writes business logic. Components coordinate through events. Everything is debuggable.

**Result:** 10x reduction in code to maintain, 10x increase in productivity, 100% clarity on what's happening.

### 🔥 The Event System - Our Secret Weapon

**This is what makes Swap different from every other framework.**

#### Why Events Are THE Foundation

Every other HTMX framework/tool makes you manually coordinate:
- ❌ Create product → manually trigger list refresh
- ❌ Delete item → manually update count badge
- ❌ Update status → manually refresh dependent views
- ❌ Show notification → manually coordinate with other updates

**With Swap's event system:**
```csharp
// Controller - just emit the domain event
await _eventBus.EmitAsync(SwapEvents.Entity.Created("product"));

// Event chains (configured once) automatically:
// product.created → list.refresh → stats.update → ui.showToast

// Components declare what they listen to:
// <div data-swap-events="product.created,product.updated">

// Server only sends events with active listeners (zero waste)
```

#### Why Static Events Change Everything

**Problem with magic strings:**
```csharp
// What events exist? Who knows!
Response.HxTrigger("productCreated"); // Is it productCreated or product.created?
```

**With static event registry:**
```csharp
// IntelliSense shows all events, compile-time safety
await _eventBus.EmitAsync(SwapEvents.Entity.Created("product"));

// Typos caught at compile time
await _eventBus.EmitAsync(SwapEvents.UI.RefreshList);

// Easy to find all usages (Go to References)
// Easy to rename safely (Rename Symbol)
// Easy to debug (set breakpoints on event class)
```

#### Debuggability Is The Killer Feature

**Built-in debugging:**
```javascript
// In browser console
window.swapEventDebug()
// Shows: All components, all events, what's listening, what's firing

// Server logs
[SwapEvents] Emitted: product.created → Resolved to: [list.refresh, stats.update]
[SwapEvents] Filtered: 2/2 events have active listeners (100% efficiency)
```

**This is what developers will love about Swap.**

---

## 🗺️ The Roadmap - Tested Increments

Note: Previous Phase 0 and Phase 1 have been removed to focus immediately on the Event System foundation. Components will be delivered as templates (source you own), not as a framework package.

### Phase 2: Event System Foundation (Shipped as 0.3.0, ongoing polish)
**Timeline:** shipped in 0.3.0  
**Goal:** Ship event system that solves real coordination problems (complete for OneHop/Bidirectional/Transitive); continue polish

> **⚠️ CRITICAL: This is THE most important phase. Everything else builds on events.**

#### The Core Insight

**Problem:** When you create a product, you need to:
1. Refresh the product list
2. Update the stats panel
3. Show a toast notification
4. Close the modal

**Current Solution:** Manually trigger 4 separate events, hope they all work.

**Better Solution:** Emit one event (`product.created`), let the framework coordinate.

#### Design Principles

**1. Static Events - Compile-Time Safety**
```csharp
// ✅ GOOD - IntelliSense, compile-time checking
await _eventBus.EmitAsync(SwapEvents.Entity.Created("product"));

// ❌ BAD - Magic strings, runtime errors
await _eventBus.EmitAsync("product.created");
```

**2. Debuggable - Clear Event Flow**
```csharp
// Server logs exactly what happened
[SwapEvents] product.created emitted
[SwapEvents] Resolved chain: [list.refresh, stats.update, ui.showToast]
[SwapEvents] Filtered to active: list.refresh, ui.showToast (2/3)
[SwapEvents] Sent: HX-Trigger: {"list.refresh":null,"ui.showToast":{"message":"Created!"}}
```

**3. Zero Waste - Only Active Listeners Get Events**
```html
<!-- Component declares subscriptions -->
<div data-swap-component="product-list"
     data-swap-events="product.created,product.updated,product.deleted">
```
Server receives: `X-Swap-Events: product.created,product.updated,product.deleted`  
Server only sends events that are in this list. No wasted triggers.

#### Delivered (0.3.0)
- Core event bus with chain resolution and filtering
- ChainResolutionMode + MaxTransitiveDepth
- Dev endpoints (events, events.json/meta.json, explain)
- CLI commands (list/from-server/validate/graph)
- Tests and documentation across framework + tools + wiki

#### Next (Polish/Optional)
- Per-root resolution overrides (safe union per response)
- Precomputed reverse index for Bidirectional performance on large graphs
- Direction policies (allow/forbid) – evaluate after usage

**Week 1: Core Event Bus with Static Events**
1. Create `SwapEvents` static class with all standard events:
   ```csharp
   public static class SwapEvents
   {
       public static class UI
       {
           public const string RefreshList = "ui.refreshList";
           public const string ShowToast = "ui.showToast";
           // ... more
       }
       
       public static class Entity
       {
           public static string Created(string entity) => $"{entity}.created";
           public static string Updated(string entity) => $"{entity}.updated";
           // ... more
       }
       
       // Auth, Notification, Cache, etc.
   }
   ```

2. Implement `ISwapEventBus` interface:
   ```csharp
   public interface ISwapEventBus
   {
       Task EmitAsync(string eventName, object? payload = null);
       HashSet<string> GetActiveEvents(HttpContext context);
   }
   ```

3. Add to `Swap.Htmx` package
4. Wire into `SwapController`
5. Add comprehensive unit tests

**Week 2: Event Chains with Debugging**
1. Implement chain configuration:
   ```csharp
   builder.Services.AddSwapHtmx(events =>
   {
       events.Chain(SwapEvents.Entity.Created("product"))
           .To(SwapEvents.UI.RefreshList)
           .To(SwapEvents.UI.ShowToast)
           .To("stats.updated"); // Can still use custom strings
   });
   ```

2. Build chain resolver with deduplication
3. Add filtering logic (only emit to active listeners)
4. Add extensive logging at every step
5. Integration tests with sample controller

**Week 3: Client-Side Event Registry**
1. Create `swap-events.js` with registry:
   - Component registration
   - MutationObserver for DOM changes
   - HTMX request interceptor (adds X-Swap-Events header)
   
2. Auto-registration on page load:
   ```javascript
   document.querySelectorAll('[data-swap-component]').forEach(el => {
       window.SwapEvents.autoRegisterElement(el);
   });
   ```

3. Browser developer tools:
   ```javascript
   // Console helper
   window.swapEventDebug() // Shows all components, events, listeners
   ```

#### Battle Test

**Build a blogging platform with:**
- Article CRUD (test basic event chains)
- Comment system (test nested events - article.commentAdded → article.refreshComments + stats.update)
- Like button (test optimistic UI + event coordination)
- Tag management (test multiple event chains from single action)

**Focus Areas:**
- Are static events discoverable? (IntelliSense works?)
- Is debugging event flow clear? (Can you trace what happened?)
- Does filtering prevent wasted events? (Measure what gets sent vs what's active)
- Are standard events comprehensive enough? (Do we need custom events often?)
- Is the client-side registry reliable? (Does it track components correctly?)

**Key Metrics:**
- Event chain configuration takes <5 minutes
- Debugging shows complete event flow in logs
- >90% of events sent have active listeners (waste <10%)
- Developers rarely need custom events (standard covers 80%+ use cases)

#### Success Criteria
- ✅ Static events provide IntelliSense and compile-time safety
- ✅ Event chains reduce manual coordination code by >80%
- ✅ Debugging shows clear event flow (server logs + browser console)
- ✅ Zero wasted events (only active listeners get events)
- ✅ Battle test proves this is easier than manual coordination
- ✅ Developers report "This is magic, but I understand how it works"

**Deliverables:**
- `Swap.Htmx` v0.4.0 with event system
- `SwapEvents` static class with 50+ standard events
- `swap-events.js` client library
- Blog platform demo app
- Complete documentation with debugging guide
- Video walkthrough of event system debugging

#### Immediate Next Steps (before templates)

- Publish user-facing docs in the wiki (Docusaurus):
    - Features → Event System: overview, concepts, contracts, testing guidance
    - CLI Reference → Events: planned commands (`events init`, `events chain`, `events add`, `events ls`, `events demo`)
- Ship event system wiring by default in all templates:
    - Program.cs: `AddSwapHtmx(...)` with sample chains; `UseSwapHtmx()` middleware
    - Include client snippet to send `X-Swap-Events`
- Implement CLI support (incremental):
    - `swap events init` (idempotent edits to Program.cs + add chains file)
    - `swap events chain` (append to central chains file)
    - `swap events ls` (introspect known events/chains)
    - Optional: `swap events demo` to scaffold endpoints + tests

---

### Phase 3: Architecture Templates (v0.4.0 → v0.5.0)
**Timeline:** 3 weeks  
**Goal:** Provide multiple starting architectures beyond monolith

#### The Core Insight

**Problem:** Every app starts with `swap new MyApp` and gets a monolith. What if you want layered architecture? Modular monolith? Microservices?

**Solution:** Multiple templates, choose upfront.

#### Tasks

**Week 1: Layered Architecture Template**
1. Create `templates/layered/` structure:
   ```
   /Presentation     - Controllers, Views
   /Application      - Services, DTOs
   /Domain          - Entities, Interfaces
   /Infrastructure  - DbContext, Repositories
   ```
2. Update CLI: `swap new MyApp --template layered`
3. Add documentation on when to use layered vs monolith

**Week 2: Modular Monolith Template**
1. Create `templates/modular/` structure:
   ```
   /Modules
       /Products
           /Controllers
           /Services
           /Views
       /Orders
       /Customers
   ```
2. Add module isolation patterns
3. Create `swap module add Orders` command

**Week 3: Documentation & Polish**
1. Write "Choosing an Architecture" guide
2. Add migration guides (monolith → layered, etc.)
3. Video walkthrough of each template

#### Battle Test

**Build three versions of the same e-commerce app:**
1. Monolith version
2. Layered version
3. Modular monolith version

**Compare:**
- Which feels best for different sizes?
- Where does each struggle?
- How hard is it to migrate between them?

#### Success Criteria
- ✅ Clear guidance on which template to choose
- ✅ Each template has working examples
- ✅ Migration paths exist and work
- ✅ Modules feel genuinely isolated (modular template)

---

### Phase 4: Component Gallery & Documentation (v0.5.0 → v0.6.0)
**Timeline:** 2 weeks  
**Goal:** Make components discoverable and delightful to use

#### The Core Insight

**Problem:** Developers don't know what components exist or how to use them without reading source code.

**Solution:** Build a Storybook-style gallery with live previews and copy-paste examples.

#### Tasks

**Week 1: Component Gallery Site**
1. Extend wiki (Docusaurus) with component section
2. Add live preview for each component (iframe demos)
3. Show code snippets with copy button
4. List all props/parameters with types

**Week 2: Enhanced CLI**
1. `swap component list` - Show all available components
2. `swap component preview table` - Open browser with live preview
3. `swap component search pagination` - Find components by keyword
4. Better `swap add component` with auto-configuration

#### Battle Test

**Give Swap to 3 developers who've never seen it:**
- Can they find and use components without asking questions?
- What components do they look for that don't exist?
- Where do they get stuck?

#### Success Criteria
- ✅ Developers find components in <1 minute
- ✅ Examples work copy-paste
- ✅ CLI preview actually helps
- ✅ Identify 5 new components to build based on feedback

---

### Phase 5: Advanced Event Features (v0.6.0 → v0.7.0)
**Timeline:** 2 weeks  
**Goal:** Add real-time capabilities and advanced event patterns

#### The Core Insight

**Problem:** Some features need real-time updates (notifications, collaborative editing, live dashboards).

**Solution:** Integrate WebSockets/SignalR with the event system.

#### Tasks

**Week 1: WebSocket Integration**
1. Create `Swap.WebSockets` package
2. Add `SwapHub` base class (wraps SignalR)
3. Wire events to WebSocket connections:
   ```csharp
   events.Chain("product.created")
       .ToBroadcast("productList") // WebSocket group
   ```

**Week 2: Advanced Event Patterns**
1. Event batching (combine multiple rapid events)
2. Event priorities (critical vs low priority)
3. Conditional events (emit only if condition met)
4. Event debugging UI (development mode inspector)

#### Battle Test

**Build a real-time dashboard:**
- Live order updates
- User presence (who's online)
- Collaborative editing (multiple users)
- Live notifications

**Focus:**
- Is WebSocket integration seamless?
- Do advanced patterns solve real problems?
- Is debugging UI helpful?

#### Success Criteria
- ✅ WebSocket setup takes <5 minutes
- ✅ Real-time updates work out of the box
- ✅ Advanced patterns reduce custom code
- ✅ Debugging UI catches issues fast

---

## 🎯 Success Metrics - How We Know It's Working

### Developer Experience Metrics

**Time to "Hello World":**
- Current: ~15 minutes (including .NET install)
- Target: <5 minutes

**Time to First Feature:**
- Current: ~30 minutes (create project, add resource, customize)
- Target: <10 minutes

**Lines of Code Reduction:**
- Current: ~500 lines per CRUD feature
- Target: <50 lines (with components + events)

**Developer Satisfaction (Survey):**
- "Would you use Swap for your next project?" → Target: >80% yes
- "How likely are you to recommend Swap?" → Target: >8/10 (NPS)

### Adoption Metrics

**Downloads:**
- Track NuGet package downloads
- Track GitHub stars/forks
- Track CLI installs

**Community:**
- GitHub issues/discussions activity
- Discord members (if we create one)
- Blog posts/videos from community

**Real Apps:**
- Track apps built with Swap (opt-in telemetry or showcase)
- Case studies from production deployments

### Quality Metrics

**Test Coverage:**
- Framework: >90% coverage
- CLI: >80% coverage
- Components: >85% coverage

**Performance:**
- Page load: <200ms (server-rendered)
- Event processing: <10ms overhead
- Component rendering: <50ms

**Bug Rate:**
- <5 critical bugs per release
- <10 major bugs per release
- <48 hour critical bug fix time

---

## 🚧 What We're NOT Building

To stay focused, explicit scope boundaries:

### ❌ Out of Scope (For Now)

**1. SPA Support**
- We're server-rendered first
- HTMX is the interaction model
- If you need React/Vue, Swap isn't for you (yet)

**2. Mobile Apps**
- Web-first
- Maybe PWA in the future
- Not building Xamarin/MAUI/React Native

**3. Visual Designers**
- Code-first always
- No drag-and-drop UI builders
- Components are code, not config

**4. Enterprise Kitchen Sink**
- Not building SAP/Salesforce competitor
- Not adding features "just in case"
- Only proven, battle-tested patterns

**5. Database-First**
- Code-first with EF Core
- Migrations over GUI designers
- Schema is code

### ✅ Might Add Later (Not Now)

**1. REST API Generation**
- Currently HTMX-first
- Could add API endpoints as secondary

**2. GraphQL Integration**
- Interesting but not core to HTMX approach

**3. CMS Features**
- Might extract from blog battle test

**4. Admin Panel Generator**
- Could be a specialized template

**5. Multi-Tenancy Patterns**
- Needs battle testing first

---

## 🧪 Battle Testing Philosophy

### The Process

**1. Identify Feature Need**
- What problem does this solve?
- Who asked for this?
- Is there a workaround?

**2. Design Minimal API**
- Simplest API that could work
- Focus on DX (what feels good?)
- Write usage code first, then implement

**3. Build Real App**
- Not a toy example
- Actually deploy it
- Use it for real work

**4. Extract Learnings**
- What worked well?
- What was annoying?
- What would you change?

**5. Refine & Ship**
- Improve based on learnings
- Document the happy path
- Ship with confidence

### Example: Table Component Battle Test

**Week 1:**
```csharp
// Initial design
<partial name="_Table" model="Model.Products" />
```
Build inventory app, discover problems:
- Need custom column formatting
- Sorting state is confusing
- Pagination doesn't preserve search

**Week 2:**
```csharp
// Refined design
<partial name="_Table" model="new TableModel {
    Items = Model.Products,
    Columns = new[] {
        new Column("Name") { Sortable = true },
        new Column("Price") { Format = "C2" },
        new Column("Actions") { Template = "_Actions" }
    },
    Sort = Model.Sort,
    Pagination = Model.Pagination
}" />
```
Build blog app, confirm it works well.

**Week 3:**
- Write documentation
- Add to component gallery
- Ship it

---

## 📅 Release Schedule

### v0.4.0 (January 2026)
- Event system foundation
- Event chains
- Client-side registry

### v0.5.0 (February 2026)
- Layered architecture template
- Modular monolith template
- Module management commands

### v0.6.0 (March 2026)
- Component gallery
- Enhanced CLI for components
- Preview commands

### v0.7.0 (April 2026)
- WebSocket integration
- Advanced event patterns
- Real-time features

### v1.0.0 (June 2026)
- Production-ready
- Full documentation
- Case studies
- Community launch

---

## 🤝 Community & Feedback

### How Developers Help Us Build

**1. Early Adopters**
- Use pre-release versions
- Report friction points
- Share what you built

**2. Battle Testers**
- Build real apps with new features
- Document what works/doesn't
- Suggest improvements

**3. Contributors**
- Fix bugs
- Add components
- Improve documentation

**4. Evangelists**
- Write blog posts
- Create videos
- Answer questions

### Feedback Loops

**Fast Feedback (Days):**
- GitHub issues for bugs
- Discord for quick questions
- Twitter/X for announcements

**Medium Feedback (Weeks):**
- Battle test reports from early adopters
- Survey after each release
- Component requests

**Slow Feedback (Months):**
- Production app case studies
- Long-term DX assessment
- Framework architecture reviews

---

## 🎓 Learning From Other Frameworks

### What We'll Steal (Learn From)

**From Rails:**
- ✅ Powerful CLI (`rails generate`)
- ✅ Convention over configuration
- ✅ Opinionated but flexible
- ✅ Great documentation

**From Laravel:**
- ✅ Elegant API design
- ✅ "Artisan" CLI quality
- ✅ Community-first approach
- ✅ Laracasts-style learning

**From Django:**
- ✅ Admin panel (maybe later)
- ✅ Batteries-included philosophy
- ✅ Great error messages

**From Next.js:**
- ✅ Templates for different architectures
- ✅ "Create Next App" experience
- ✅ Developer preview tools

**From Tailwind/Shadcn:**
- ✅ Component copy-paste experience
- ✅ Beautiful documentation
- ✅ Customization without breaking

### What We'll Avoid

**From Complex Frameworks:**
- ❌ Too many abstractions (ABP Framework)
- ❌ Magical base classes hiding behavior
- ❌ Configuration over code

**From Minimal Frameworks:**
- ❌ Too unopinionated (Express.js)
- ❌ No guidance on structure
- ❌ Reinvent everything yourself

**From JavaScript Frameworks:**
- ❌ Build step complexity
- ❌ Constant breaking changes
- ❌ Fatigue from too many choices

---

## 💭 Long-Term Vision (2+ Years)

### Where Swap Could Go

**1. Swap Ecosystem**
- Third-party component marketplace
- Community templates
- Plugin system for extensions

**2. Swap Cloud (Maybe)**
- One-click deployments
- Managed hosting optimized for Swap
- Development preview environments

**3. Swap Studio (Visual Tools)**
- Not a code generator, but helper tools
- Component preview/playground
- Database schema visualizer
- Event flow debugger

**4. Swap Enterprise**
- Multi-tenancy patterns
- Advanced security features
- Compliance templates (HIPAA, GDPR)
- SLA support options

### The North Star

**In 2027, we want developers to say:**

> "I can build production apps 10x faster with Swap than anything else. The framework stays out of my way, the components are beautiful, and the CLI makes everything effortless. When I need to customize, I just read the generated code—no magic, no surprises."

---

## 🎬 Getting Started (For Contributors)

### How to Help Build Swap

**1. Use It**
- Build something with current Swap
- Report what's annoying
- Suggest improvements

**2. Battle Test**
- Pick a phase from roadmap
- Build the battle test app
- Document learnings

**3. Contribute Code**
- Pick an issue from GitHub
- Follow coding standards
- Add tests

**4. Improve Docs**
- Fix typos/errors
- Add examples
- Create tutorials

**5. Share**
- Write blog posts
- Create videos
- Answer questions

### Contact & Communication

- **GitHub:** Issues, Discussions, PRs
- **Email:** (Add when ready)
- **Discord:** (Add when community grows)
- **Twitter/X:** (Add for announcements)

---

## 📝 Document Maintenance

**This roadmap is a living document.**

**Updated:** After each battle test  
**Reviewed:** Monthly  
**Revised:** When we learn something important

**Current Status:**
- ✅ v0.2.0-dev shipped
- 🟡 v0.2.1 planned
- 🔵 v0.3.0 designed
- ⚪ v0.4.0+ vision stage

**Legend:**
- ✅ Shipped
- 🟡 In progress
- 🔵 Designed/planned
- ⚪ Vision/idea

---

**Last Updated:** November 1, 2025  
**Next Review:** December 1, 2025  
**Owner:** @jdtoon

---

*"Build what developers love. Battle test everything. Ship with confidence."*
