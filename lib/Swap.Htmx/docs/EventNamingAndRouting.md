# Event Naming & Realtime Routing Conventions

This guide clarifies the difference between:

- **Swap (domain/UI) events** — the names you trigger via `HX-Trigger` (usually dot-separated).
- **Realtime routing keys** — internal event keys prefixed with `sse:` (and legacy `realtime:`) that tell the realtime bridge *how* to broadcast.
- **Client realtime event names** — the `event:` name sent over SSE (and used by `sse-swap=`), *not* the internal `sse:` routing key.

If you keep these distinct, it becomes much easier to reason about “what fires”, “what broadcasts”, and “what swaps”.

---

## 1) Swap (Domain/UI) Event Names

These are the normal events you use throughout Swap.Htmx:

- Trigger from the server via `.WithTrigger(...)` (adds `HX-Trigger`).
- Listen on the client via `hx-trigger="... from:body"`.
- Configure server-side chains via `events.When(...)`.

**Recommendation:** use dot-separated, lowercase names (kebab-case also works if you prefer it consistently):

- `task.created`
- `task.updated`
- `dashboard.activity.logged`

Example:

```csharp
public static class TaskEvents
{
    public static readonly EventKey Created = new("task.created");
}

return SwapResponse()
    .WithTrigger(TaskEvents.Created, new { id = task.Id })
    .Build();
```

Client listening example:

```html
<div
  hx-get="/tasks/list"
  hx-trigger="task.created from:body">
</div>
```

---

## 2) Realtime Routing Keys (`sse:` / `realtime:`)

Realtime broadcasting is driven by *internal* routing keys.

- These keys **must start with** `sse:` (or legacy `realtime:`).
- They are **not** meant to be used in HTML.
- They encode routing like broadcast vs room vs user.

Examples of internal routing keys:

- `sse:broadcast:task.created`
- `sse:room:project-123:task.updated`
- `sse:user:user-123:notification.new`

You usually create these keys via helpers:

- Event chain helpers like `.Broadcast("all")` or `.Broadcast("room:...")`
- `SseEvents.*` (`SseEvents.Broadcast(...)`, `SseEvents.Room(...)`, `SseEvents.User(...)`, ...)

---

## 3) Client Realtime Event Names (SSE `event:` and `sse-swap=`)

For SSE, the client swaps based on the **SSE event name** (the `event:` field), and HTMX’s SSE extension matches that name:

```html
<div hx-ext="sse" sse-connect="/swap/sse">
  <div id="activity" sse-swap="dashboard.activity.logged"></div>
</div>
```

Important:

- `sse-swap="..."` must match the **SSE event name** exactly.
- `sse-swap` does **not** include the internal `sse:` prefix.

---

## Common Patterns

### Pattern A (Simplest): use the same name everywhere

If you call `.Broadcast()` on a Swap event chain, Swap.Htmx uses the **source event name** as the realtime event name.

```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.When(TaskEvents.Created)
        .RefreshPartial("task-list", "_TaskList")
        .Broadcast();
});
```

- Trigger name: `task.created`
- SSE/WS event name: `task.created`
- Client uses `sse-swap="task.created"`

### Pattern B (Explicit mapping): domain event → realtime event name

Use `ChainToSse(...)` when you want a different frontend event name (or different routing):

```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.ChainToSse(TaskEvents.Created, SseEvents.Broadcast("tasks.list.refresh"));
});
```

Now:

- Domain/UI event: `task.created`
- SSE event name: `tasks.list.refresh`
- Client uses `sse-swap="tasks.list.refresh"`

### Pattern C (Escape hatch): broadcast HTML directly

Use the registry when you need dynamic targeting or already have HTML:

```csharp
await registry.BroadcastToRoomsAsync(
    eventName: "tasks.list.refresh",
    html: renderedHtml,
    rooms: ["project-123"],
    cancellationToken);
```

---

## Summary

- Use dot-separated names for Swap events (`EventKey`) and for client listeners.
- Treat `sse:` / `realtime:` as **internal routing prefixes** only.
- Treat SSE `event:` names (and `sse-swap`) as **client-facing** names.
