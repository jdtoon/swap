# Event System – CLI Integration (DX Plan)

This document proposes CLI commands and wiring to make the event system easy to adopt and consistent across apps.

## Goals

- Zero-config setup for new apps
- One-liners to add event-aware endpoints and client wiring
- Discoverability (list known events, chains, and listeners)
- Debuggability (runtime inspection, logs, test scaffolds)

## Proposed commands

- `swap events init`
  - Adds `AddSwapHtmx(...)` and `UseSwapHtmx()` to Program.cs if missing
  - Creates an `Events` folder with `SwapEvents` extension class if not present
  - Adds sample chains (commented) and a README link

- `swap events add <domain>`
  - Adds strongly-typed helpers under `SwapEvents.Entity` (e.g., `Created/Updated/Deleted` for `<domain>`)
  - Option: `--ui refreshList,showToast` to append default chains

- `swap events chain <from> --to <ui1,ui2>`
  - Appends chain configuration in a central place (e.g., `Events/SwapEventChains.cs`)

- `swap events ls`
  - Lists all known events (domain + UI) and configured chains

- `swap events demo`
  - Generates demo endpoints (Create/Extreme/Collision) for quick validation
  - Generates test scaffolds using `Swap.Testing` with sample assertions

## Templates and files

- `Events/SwapEventChains.cs`
  - Static class where CLI writes chain config
- `Events/README.md`
  - Quick reference linking to full docs
- Client template (`wwwroot/js/swap-events.js`)
  - Sends `X-Swap-Events` for active components; CLI can add tag or layout snippet

## Test scaffolding

- Generate `*Tests` project references to `Swap.Testing`
- Sample tests:
  - filtered/unfiltered behavior, collisions, bad request, redirect via HX-Redirect

## DX touches

- Add `swap doctor` checks for middleware registration and missing headers
- Add `swap logs events` to pretty-print `[SwapEvents] Emitted/Filtered/Active` entries

## Next steps

- Implement `swap events init`, `ls`, and `chain` first (low risk, high value)
- Add demo/test scaffolding behind `swap events demo` flag
- Keep file edits idempotent and safe (parse/patch Program.cs)
