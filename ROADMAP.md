# Swap Roadmap

This document outlines the future direction of the Swap library, focusing on architectural hardening, developer experience, and ecosystem expansion.

## Phase 1: Hardening & Architecture (Immediate Focus)

**Goal:** Solidify the core architecture, remove technical debt, and ensure production readiness.

- [x] **Refactor Server Events Structure**
    - Merge `lib/Swap.Htmx/ServerEvents` and `lib/Swap.Htmx/ServerSentEvents` to reduce confusion.
    - Establish a clear distinction between the Abstraction (e.g., `Realtime` or `Transport`) and the Implementation (`ServerSentEvents`).
- [x] **Abstract Session Dependency**
    - Remove the hard dependency on `HttpContext.Session` in `SwapController`.
    - Introduce an `ISwapUserContext` or `ISwapIdentityService` abstraction to allow pluggable ID resolution (Cookie, JWT, Session, etc.).

## Phase 2: Source Generators (Type Safety)

**Goal:** Eliminate "magic strings" for events and provide a robust, type-safe developer experience.

- [x] **Event Source Generator**
    - Create a Roslyn Source Generator that reads a simple definition (e.g., `Events.json` or partial classes) and generates static `EventKey` hierarchies.
    - **Benefit:** Compile-time safety, IntelliSense support (`Events.User.Created`), and automatic refactoring.
    - **Output:** `public static readonly EventKey Created = new("user.created");`

## Phase 2.5: Project Templates (Developer Experience)

**Goal:** Eliminate setup friction. Provide "batteries-included" templates that follow Swap best practices (Clean UI, LibMan, Source Generators).

- [ ] **Create Template Pack (`Swap.Templates`)**
    - **`swap-mvc`**: Clean MVC project (No jQuery/Bootstrap), pre-configured with Swap + HTMX.
    - **`swap-razor`**: Clean Razor Pages project.
    - **`swap-min`**: Minimal API project.
- [ ] **Smart Referencing**
    - Implement logic to switch between `<PackageReference>` (for users) and `<ProjectReference>` (for internal repo demos) via a `--local` flag.

## Phase 3: Realtime & HTMX Protocol Mastery

**Goal:** Expand realtime capabilities beyond SSE and provide full server-side control over HTMX behavior.

- [ ] **WebSockets / SignalR Transport**
    - Abstract `ISwapEventPublisher` to support multiple transports.
    - Implement a WebSocket/SignalR transport to enable **bi-directional** communication.
    - **Benefit:** Allows the client to trigger server events directly (e.g., "UserTyping") which can then be broadcasted.

- [ ] **HTMX Protocol Gaps (Fluent API)**
    - **Server-Side Retargeting:**
        - `Retarget(selector)`: Override `hx-target` from the server.
        - `Reswap(strategy)`: Override `hx-swap` from the server.
    - **Navigation Control:**
        - `PushUrl(url)`: Update browser history without full reload.
        - `ReplaceUrl(url)`: Update URL bar silently.
        - `RefreshPage()`: Trigger a full page refresh via `HX-Refresh`.
    - **Client-Side Triggers:**
        - `TriggerAfterSwap(event)`: Trigger events after DOM update.
        - `TriggerAfterSettle(event)`: Trigger events after animations settle.
    - **Status Code Helpers:**
        - `StopPolling()`: Return 286 to stop HTMX polling.
        - `DoNothing()`: Return 204 to tell HTMX to do nothing.
    - **Advanced OOB:**
        - `WithOob(o => o.Append(...))`: Fluent API for complex Out-of-Band swaps (append, prepend, delete).
