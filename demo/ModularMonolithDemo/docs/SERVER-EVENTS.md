# Server Events (Domain Chains)

This app supports two event planes:

- UI Events (Swap.Htmx): client-facing, best-effort partial updates
- Server Events (Domain Chains): cross-module reactions, policies, and backend orchestration

## When to use which?

- Use UI events to keep the page fresh after an action (refresh lists, show toasts, update widgets).
- Use server events to notify other modules about domain changes and run logic without compile-time coupling.

## Publishing server events

From inside a controller or service in your module:

```csharp
public class TodosUiController : Controller
{
    private readonly IEventChainRegistrar _events;
    public async Task<IActionResult> Add(string title)
    {
        var item = _service.Add(title);
        await _events.PublishAsync(TodoEvents.Domain.Created, new { id = item.Id }, HttpContext.RequestServices);
        return NoContent();
    }
}
```

Also emit a UI event for on-page updates as needed via `ISwapEventBus`.

## Subscribing to server events

In your module implementation (IModule.ConfigureEventChains):

```csharp
public void ConfigureEventChains(IEventChainRegistrar registrar)
{
    registrar.Register<object>(TodoEvents.Domain.Created, async (payload, sp) =>
    {
        var queries = sp.GetRequiredService<IDemoQueries>();
        queries.AppendActivity("Todo created");
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
