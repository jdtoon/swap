---
id: events
title: Events (DX-first)
slug: /cli/events
---

DX-focused commands to wire and manage the Event System, with smart defaults that actually save time.

> Status: Planned. The APIs below are proposed and may change.

---
id: events
title: Events (Planned)
slug: /cli/events
---

DX-focused commands to wire and manage the Event System.

> Status: Planned. The APIs below are proposed and may change.

## Commands (proposed)

- `swap events init`
  - Add `AddSwapHtmx(...)` and `UseSwapHtmx()` to Program.cs if missing
  - Create `Events/SwapEventChains.cs` for chain configuration
  - Add client snippet to send `X-Swap-Events`

- `swap events chain <from> --to <ui1,ui2>`
  - Append chains to `Events/SwapEventChains.cs`

- `swap events add <domain>`
  - Add typed helpers under `SwapEvents.Entity` (Created/Updated/Deleted)

- `swap events ls`
  - List known events (domain + UI) and chains

- `swap events demo`
  - Generate demo endpoints and tests using `Swap.Testing`

## Examples

```bash
swap events init
swap events add product --ui refreshList,showToast
swap events chain product.created --to ui.refreshList,ui.showToast
swap events ls
```

## Notes

- All templates will ship with Event System wiring by default
- Suppression policy for non-2xx/redirect may be configurable in future
- Creates Razor partials + JS mount that automatically sends `X-Swap-Events` for declared `--listens`
