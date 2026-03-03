# Swap Framework — Workspace Instructions

## Project Overview

Swap is a .NET HTMX framework (packages: `Swap.Htmx`, `Swap.Htmx.Realtime`, `Swap.Htmx.Realtime.Redis`, `Swap.Testing`). The solution targets `net8.0`, `net9.0`, and `net10.0`. Version is centralized in `Directory.Build.props`.

## Golden Rule

**Nothing ships broken.** Every change — feature, fix, refactor, demo update — must leave the solution in a fully building, fully tested, fully working state. No exceptions.

## Build Verification (Mandatory After Every Change)

Run after every code change, no matter how small:

```
dotnet build swap.sln --configuration Release
dotnet test --configuration Release --verbosity normal
```

If either command fails, the change is not complete. Fix it before moving on.

## Project Structure

| Path | Purpose |
|------|---------|
| `lib/Swap.Htmx/` | Core framework library |
| `lib/Swap.Htmx.Realtime/` | SSE/WebSocket realtime package |
| `lib/Swap.Htmx.Realtime.Redis/` | Redis backplane for distributed SSE |
| `lib/Swap.Testing/` | Integration testing library (HtmxTestClient, assertions) |
| `lib/Swap.Htmx.Tests/` | Unit tests for core library |
| `lib/Swap.Htmx.Benchmarks/` | Performance benchmarks |
| `framework/Swap.Htmx.Generators/` | Roslyn source generators |
| `framework/Swap.Htmx.Generators.Tests/` | Generator tests |
| `demo/` | Demo applications showcasing framework features |
| `templates/` | `dotnet new` project templates |

## Code Conventions

- No external NuGet dependencies in `Swap.Htmx` core — framework references only
- Source generators are auto-injected via `Directory.Build.props` — do not add manually
- Use `EventKey` struct (not raw strings) for event identifiers
- Use `SwapView()` / `SwapResponse()` / `SwapEvent()` patterns — never raw `ViewResult` in Swap controllers
- Suppress analyzer `SWAPHTMX001` only in test projects (already configured in `.editorconfig`)

## Testing Requirements

Every change to `lib/` or `framework/` must include corresponding tests:

- **Unit tests** go in `lib/Swap.Htmx.Tests/` or `framework/Swap.Htmx.Generators.Tests/`
- **Integration tests** use `Swap.Testing` (`HtmxTestClient`, `HtmxTestResponse` assertions)
- **Public API changes** must update snapshot tests in `lib/Swap.Htmx.Tests/PublicApiSnapshots/`
- Tests must pass on all target frameworks (`net8.0`, `net9.0`, `net10.0`)

## Demo Applications

Demos in `demo/` are living documentation. They must always build and run:

- When a framework feature changes, update affected demos to match
- When a new feature is added, either update an existing demo or create a new one
- Each demo has a `README.md` explaining what it showcases — keep these current
- Demos are not in the solution file — build them individually: `dotnet build demo/<name>/<name>.csproj`

## Templates

Templates in `templates/` generate starter projects. CI smoke-tests 6 template variants:

- `swap-mvc` (with/without SSE)
- `swap-modular` (with/without SSE, with/without sample module)

Template changes must pass the smoke test matrix. If a framework API changes, update templates to match.

## Branching and CI

- CI runs on push to `develop` and `main`, and on PRs to both
- CI pipeline: restore → build → test → pack → template smoke tests
- All checks must be green before considering work complete
