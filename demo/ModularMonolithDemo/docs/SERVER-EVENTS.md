# Server Events (Domain Chains)

This app supports two event planes:

- UI Events (Swap.Htmx): client-facing, best-effort partial updates
- Server Events (Domain Chains): cross-module reactions, policies, and backend orchestration

## When to use which?

- Use UI events to keep the page fresh after an action (refresh lists, show toasts, update widgets).
- Use server events to notify other modules about domain changes and run logic without compile-time coupling.

## Publishing server events

From inside a controller or service in your module. Prefer typed payloads for AOT-friendly, unambiguous deserialization:

```csharp
public class TodosUiController : Controller
{
    private readonly IEventChainRegistrar _events;
    public async Task<IActionResult> Add(string title)
    {
        var item = _service.Add(title);
        await _events.PublishAsync(
            TodoEvents.Domain.Created,
            new ModularMonolithDemo.Modules.Todos.Contracts.TodoEventPayloads.Created(item.Id),
            HttpContext.RequestServices);
        return NoContent();
    }
}
```

Also emit a UI event for on-page updates as needed via `ISwapEventBus`.

## Subscribing to server events

In your module implementation (IModule.ConfigureEventChains). Use the same typed payloads you published:

```csharp
public void ConfigureEventChains(IEventChainRegistrar registrar)
{
    registrar.Register<ModularMonolithDemo.Modules.Todos.Contracts.TodoEventPayloads.Created>(
        TodoEvents.Domain.Created,
        async (payload, sp) =>
        {
            var queries = sp.GetRequiredService<IDemoQueries>();
            queries.AppendActivity($"Todo created #{payload.Id}");
            await Task.CompletedTask;
        });
}
```

## Transports

Two modes are available:

- InMemory (default): single-process, thread-safe; great for local dev and tests.
- RabbitMQ (distributed): processes subscribe via queues and exchange; suitable for multi-instance deployments.

Configure in host settings or environment:

- `ServerEvents:Transport` = `InMemory` | `RabbitMq`
- If `RabbitMq`:
  - `ServerEvents:RabbitMq:HostName`, `Port`, `UserName`, `Password`, `VirtualHost`, `ExchangeName`, optional `ClientName`

## Docker (with RabbitMQ)

The demo compose includes a `rabbitmq` service and sets environment variables for the web container. See `docker-compose.yml`.

Open RabbitMQ Management at http://localhost:15672 (guest/guest) to observe exchanges and queues.

## Diagnostics

- Active registrar/transport: `GET /dev/server-events/info` (Development only) returns the registrar and transport types currently in use.
- UI chain visuals: see Swap HTMX dev endpoints listed in `WEB-HOST.md` (Development only).

## Smoothing eventual consistency (HTMX-only one-shot refresh)

When using the RabbitMQ transport, server-event handlers run asynchronously after the HTTP response, so cross-module panels may lag briefly. A clean, JavaScript-free way to reconcile is to add a delayed HTMX retrigger alongside the immediate one.

Example for the Demo Activity Log panel:

```html
<div id="activity-panel"
    hx-get="/Demo/ActivityLog"
    hx-trigger="load, @EventNames.Ui.ActivityAppend from:body, @EventNames.Ui.ActivityAppend from:body delay:400ms"
    hx-target="this"
    hx-swap="innerHTML">
    <div class="loading loading-spinner"></div>
</div>
```

How it works:
- The panel refreshes on page load and immediately on the UI event.
- The same UI event also triggers a one-shot delayed refresh (400ms), giving the distributed handler time to complete.

Notes:
- In InMemory mode (single process), the delayed refresh is typically redundant but harmless.
- You can tune or remove the delayed refresh per panel without touching server code.
