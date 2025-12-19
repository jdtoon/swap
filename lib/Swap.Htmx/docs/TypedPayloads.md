# Typed Payloads (Recommended)

Swap supports attaching an optional payload to events (for `HX-Trigger` and realtime forwarding). Because payloads can outlive a single controller action (and may be consumed by client code, other handlers, or realtime bridges), prefer **stable DTO payload types** over anonymous objects.

## Why typed payloads

Anonymous objects are convenient, but they’re easy to accidentally break:

- Renaming a property silently breaks JavaScript consumers.
- Different call sites may use different property shapes for the same event.
- It’s hard to version and evolve payloads safely.

Typed DTOs make payload contracts explicit and refactor-safe.

## Recommended pattern

- Keep event keys and payload DTOs next to each other (same folder/namespace).
- Use small, serializable DTOs (`record` is great).
- Treat payload DTOs as part of the “contract” if the event is consumed outside a single controller.

Example:

```csharp
// Events/CartEvents.cs
public static class CartEvents
{
    public static readonly EventKey AddFailed = new("cart.addFailed");
}

// Events/CartPayloads.cs
public sealed record CartAddFailedPayload(int ProductId, string Reason);

// Controller
return SwapEvent(CartEvents.AddFailed, new CartAddFailedPayload(productId, "Product not available"))
    .Build();
```

## Versioning guidance

If an event’s payload is consumed by multiple places (multiple pages, multiple clients, realtime), avoid “breaking” changes:

- Prefer **additive** changes (add new nullable/optional properties).
- If you must break shape, create a new event key (e.g. `cart.addFailed.v2`) and migrate consumers.

## Notes

- Payloads are serialized into the `HX-Trigger` JSON object.
- Keep payloads small. Do not include secrets or user-private data unless you have a clear authorization story.

## See also

- [Events Guide](Events.md)
- [Security Checklist](SecurityChecklist.md)
- [Event Naming & Realtime Routing](EventNamingAndRouting.md)
