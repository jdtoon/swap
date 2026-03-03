---
description: "Use when creating, modifying, or reviewing demo applications in the demo/ directory. Covers demo build requirements, README standards, and the rule that demos must always build and run."
applyTo: "demo/**"
---
# Swap Demo Application Standards

## Golden Rule for Demos

Demos are living documentation. **Every demo must build, run, and work at all times.** A broken demo is a broken framework promise.

## Build Verification

Demos are NOT in the main solution file. Build them individually:

```
dotnet build demo/<DemoName>/<DemoName>.csproj
```

For demos with solution files:

```
dotnet build demo/<DemoName>/<DemoName>.sln
```

After ANY framework change, verify all affected demos still build:

```powershell
# Quick check: build all demo projects
Get-ChildItem demo -Recurse -Filter "*.csproj" | ForEach-Object {
    Write-Host "Building $($_.FullName)..."
    dotnet build $_.FullName --configuration Release
}
```

## When to Update Demos

| Framework Change | Required Demo Action |
|-----------------|---------------------|
| Public API rename/removal | Update all demos using that API |
| New feature | Update the most relevant existing demo OR create a new one |
| Behavior change | Update any demo that relied on the old behavior |
| Bug fix | If a demo was working around the bug, simplify it |

## When to Create a New Demo

Create a new demo when:

- A feature is significant enough to warrant its own showcase
- No existing demo naturally fits the new feature
- The pattern being demonstrated is complex enough to need a dedicated example

## New Demo Checklist

1. **README.md** — Every demo must have one explaining:
   - What the demo showcases (1-2 sentences)
   - How to run it (`dotnet run` command)
   - Key code locations to look at
   - Which Swap features are demonstrated
2. **Minimal dependencies** — Only add what the demo needs
3. **Self-contained** — Must run with `dotnet run` after `libman restore` (if using client libs)
4. **Working sample data** — Demos should show something meaningful on first run, not an empty page
5. **Build verified** — Must build in Release configuration

## Demo Quality Standards

- Use the **latest Swap APIs** — no deprecated patterns
- Show **idiomatic Swap code** — these are copyable examples
- Include **comments in non-obvious code** — demos teach
- Use **DaisyUI 5 + Tailwind CSS v4** for styling (consistent with templates)
- Demonstrate **error handling** where relevant (validation, server errors)

## Demo Inventory

Keep track of what each demo showcases. If a demo's purpose overlaps significantly with another, consolidate:

| Demo | Purpose |
|------|---------|
| SwapMinimal | Minimal APIs, zero controllers |
| SwapPages | Razor Pages integration |
| SwapNavDemo | SPA navigation with `<swap-nav>` |
| SwapExpenses | Source generators (`[SwapEventSource]`) |
| SwapLab | Pattern library — 15+ interactive examples |
| SwapStateDemo | SwapState patterns (filters, wizards, dashboards) |
| SwapSmallPartials | High-scale OOB coordination (50+ partials) |
| SwapShop | Three-tier API demonstration (full e-commerce) |
| SwapDebtors | Enterprise patterns (state, generators, SSE) |
| SwapPhase15 | Distributed handlers, client actions, `[SwapForm]` |
| SwapDashboard | Many-partials coordination (20+ widgets) |
| SwapChat | SSE real-time chat with auth |
| SwapWebSockets | WebSocket bidirectional communication |
| SwapRedisDemo | Distributed SSE with Redis backplane |
