---
id: events
title: Events
slug: /cli/events
---

Inspect and debug your Event System configuration from source or a running app.

## Commands

### `swap events list`

List configured chains by scanning your project source.

```
swap events list -p .
```

- Scans `Events/SwapEventChains.cs` for `Chain(...)` calls
- Resolves `EventNames.*` constants from `Events/EventNames.cs`
- Prints a table of Trigger → Chained plus summary counts

### `swap events from-server`

Fetch chains from a running app’s dev endpoint.

```
swap events from-server --url http://localhost:5000
```

- Queries `/_swap/dev/events.json` (Development only)
- Pretty-prints the Trigger → Chained mapping
- Tip: run the app in one terminal (or detached) and query from another

### `swap events validate`

Validate event names and detect cycles by scanning your source (no server required).

```
swap events validate -p .
```

- Uses the same parser as `list`
- Name rules: `^[a-z][a-z0-9]*(\.[a-z][A-Za-z0-9]*)+$`
- Exits non‑zero when issues are found

### `swap events graph`

Output a visual graph (Mermaid by default, DOT supported) for your chains.

```
swap events graph -p . --format mermaid
swap events graph -p . --format dot --output chains.dot
```

## Related DX

- Dev endpoints: `/_swap/dev/events` (HTML) and `/_swap/dev/events.json` (JSON)
- Resolution modes are configured in code via `SwapEventBusOptions.ResolutionMode` and `MaxTransitiveDepth`
