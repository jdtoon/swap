# Swap Event System

**Server-driven interactivity made declarative and testable.**

The Swap Event System is the secret sauce that makes HTMX applications declarative, maintainable, and testable. It allows you to define UI reactions once during startup, emit domain events in your controllers, and let the framework handle the wiring.

---

## 🎯 What is the Event System?

The Swap Event System bridges the gap between server-side domain logic and client-side UI reactions. Instead of manually managing HTMX headers and event triggers, you define **event chains** that map domain events to UI events.

**The Flow:**
1. **Define chains** during application startup
2. **Emit domain events** in your controllers
3. **Chain resolution** maps domain → UI events
4. **Merge into HX-Trigger** header automatically
5. **Client handles events** via HTMX markup

**Key Benefits:**
- **Declarative**: Define UI reactions in one place, not scattered across controllers
- **Testable**: Event chains can be validated and tested without touching the DOM
- **Type-Safe**: Compile-time errors prevent invalid event names
- **Observable**: Dev dashboard visualizes event chains and resolution
- **Distributed**: Optional RabbitMQ integration for cross-process events

---

## 🧩 Event Types

### UI Events (Client-Side)

These events are handled by HTMX in the browser. They appear in the `HX-Trigger` response header.

**Built-in UI Events:**
```csharp
SwapEvents.UI.RefreshList       // Trigger refresh of a list
SwapEvents.UI.ShowToast         // Show toast notification
SwapEvents.UI.CloseModal        // Close modal dialog
SwapEvents.UI.UpdateStats       // Update statistics display
SwapEvents.UI.ClearForm         // Clear form fields
```

**Custom UI Events:**
```csharp
SwapEvents.Custom("productPriceUpdated")
SwapEvents.Custom("cartItemAdded")
```

**Client-Side Handling:**
```html
<!-- Refresh product list when productCreated is fired -->
<div id="product-list" 
     hx-get="/products/list" 
     hx-trigger="productCreated from:body">
</div>

<!-- Show toast on toastSuccess -->
<div hx-trigger="toastSuccess from:body" 
     hx-swap="none" 
     hx-on::before-request="showToast(event.detail.message)">
</div>
```

---

### Domain Events (Server-Side)

Domain events represent things that happened in your application. They are emitted by controllers and resolved to UI events via chains.

**Built-in Domain Events:**
```csharp
SwapEvents.Entity.Created("product")   // Entity created
SwapEvents.Entity.Updated("order")     // Entity updated
SwapEvents.Entity.Deleted("customer")  // Entity deleted
SwapEvents.Entity.Archived("invoice")  // Entity archived

SwapEvents.Workflow.Started("checkout")
SwapEvents.Workflow.Completed("payment")
SwapEvents.Workflow.Failed("validation")

SwapEvents.Data.Imported("products")
SwapEvents.Data.Exported("orders")
```

**Custom Domain Events:**
```csharp
SwapEvents.Custom("order.shipped")
SwapEvents.Custom("inventory.low")
SwapEvents.Custom("payment.processed")
```

---

## 🔗 Event Chains

Event chains define relationships between domain events and UI events. When a domain event is emitted, the chain resolution system determines which UI events should be triggered.

### Defining Chains

**Basic Chain (Domain → UI):**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    // When product is created, refresh list and show toast
    events.Chain(
        SwapEvents.Entity.Created("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );
});
```

**Multiple Chains:**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    // Product events
    events.Chain(
        SwapEvents.Entity.Created("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );

    events.Chain(
        SwapEvents.Entity.Updated("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.Custom("productPriceUpdated")
    );

    events.Chain(
        SwapEvents.Entity.Deleted("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.CloseModal
    );
});
```

**Complex Chains (Domain → Domain → UI):**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    // Order created triggers inventory check
    events.Chain(
        SwapEvents.Entity.Created("order"),
        SwapEvents.Custom("inventory.check")
    );

    // Inventory check triggers UI updates
    events.Chain(
        SwapEvents.Custom("inventory.check"),
        SwapEvents.UI.RefreshList,
        SwapEvents.Custom("inventory.updated")
    );
});
```

---

## 🔄 Chain Resolution Modes

The event system supports three resolution modes that determine how chains are traversed.

### OneHop (Default)

Only immediate chained events are resolved.

**Example:**
```csharp
events.Chain("A", "B", "C");
events.Chain("B", "D");

// Emit "A" → Resolves to: ["B", "C"]
// Does NOT resolve to "D" (requires second hop)
```

**Use Case:** Simple, predictable event chains. Most applications.

---

### Bidirectional

Resolves both forward chains and reverse dependencies.

**Example:**
```csharp
events.Chain("A", "B");
events.Chain("C", "B");

// Emit "A" → Resolves to: ["B"]
// Emit "B" → Resolves to: ["A", "C"] (reverse dependencies)
```

**Use Case:** Refresh scenarios where updating a child should refresh parent lists.

**Configuration:**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.UseResolutionMode(ChainResolutionMode.Bidirectional);
    events.Chain(/*...*/);
});
```

---

### Transitive

Breadth-first search across the event graph up to a configurable depth.

**Example:**
```csharp
events.Chain("A", "B");
events.Chain("B", "C");
events.Chain("C", "D");

// With depth=2:
// Emit "A" → Resolves to: ["B", "C"]

// With depth=3:
// Emit "A" → Resolves to: ["B", "C", "D"]
```

**Use Case:** Complex workflows with cascading updates.

**Configuration:**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.UseResolutionMode(ChainResolutionMode.Transitive);
    events.UseMaxTransitiveDepth(3);
    events.Chain(/*...*/);
});
```

---

## 🚀 Emitting Events

### In Controllers

**Basic Emit:**
```csharp
public class ProductsController : SwapController
{
    private readonly ISwapEventBus _events;

    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        
        // Emit domain event
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), product);
        
        return SwapView(product);
    }
}
```

**Emit Multiple Events:**
```csharp
public async Task<IActionResult> CompleteOrder(int orderId)
{
    await _service.CompleteAsync(orderId);
    
    // Emit multiple domain events
    await _events.EmitAsync(SwapEvents.Workflow.Completed("order"));
    await _events.EmitAsync(SwapEvents.Custom("inventory.updated"));
    
    return SwapView();
}
```

**Emit with Payload:**
```csharp
await _events.EmitAsync(
    SwapEvents.Entity.Created("product"),
    new { 
        id = product.Id, 
        name = product.Name,
        price = product.Price 
    }
);
```

---

### Fluent Header API

For simple cases, you can also trigger events directly via the Fluent Header API:

```csharp
// Directly trigger UI events
Response.HxTrigger("productCreated");
Response.HxTrigger("productUpdated", new { id = 42 });

// Toast shortcuts
Response.ShowSuccessToast("Product saved!");
Response.ShowErrorToast("Something went wrong");
Response.ShowWarningToast("Please review");

// Other HTMX headers
Response.HxRedirect("/products");
Response.HxRefresh();
Response.HxRetarget("#product-list");
Response.HxReswap("beforebegin");
Response.HxPushUrl("/products/123");
```

---

## 🌐 Server Events (RabbitMQ)

For **modular monoliths** or **distributed deployments**, Swap supports RabbitMQ-based server events that propagate across processes.

### When to Use Server Events

- **Modular Monolith**: Modules run in the same process but need decoupled communication
- **Distributed Events**: Multiple app instances need to react to the same event
- **Background Jobs**: Workers listen for events emitted by the web app
- **Microservices**: Coordinating actions across service boundaries

### Configuration

**appsettings.json:**
```json
{
  "Swap": {
    "ServerEvents": {
      "Enabled": true,
      "ConnectionString": "amqp://guest:guest@localhost:5672",
      "ExchangeName": "swap.events",
      "QueuePrefix": "MyApp"
    }
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddSwapServerEventChainsFromConfiguration(
    builder.Configuration,
    "Swap:ServerEvents"
);
```

**Docker Compose (RabbitMQ):**
```yaml
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
```

---

### Server Event Chains

Define server event chains that listen for events from RabbitMQ:

```csharp
public class OrdersModule : IModule
{
    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // Listen for server events
        registrar.Register("order.created", async (OrderCreated evt) =>
        {
            // Handle event from RabbitMQ
            await UpdateInventoryAsync(evt.ProductId, evt.Quantity);
        });

        registrar.Register("inventory.low", async (InventoryLow evt) =>
        {
            // Trigger reorder workflow
            await _reorderService.TriggerAsync(evt.ProductId);
        });
    }
}
```

**Emitting Server Events:**
```csharp
public class OrdersController : SwapController
{
    private readonly ISwapEventBus _events;

    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        var order = await _service.CreateAsync(dto);
        
        // Emit to RabbitMQ (if server events enabled)
        await _events.EmitAsync(SwapEvents.Custom("order.created"), order);
        
        return SwapView(order);
    }
}
```

---

## 🔍 Development Dashboard

Swap includes a development dashboard for visualizing and debugging event chains.

**Available Endpoints:**

### Visual Dashboard
```
GET /_swap/dev/events
```
- **Mermaid diagram** of all event chains
- **Chain statistics** (total chains, depth, complexity)
- **Resolution mode** indicator
- **Interactive graph** (click nodes to expand)

### JSON Export
```
GET /_swap/dev/events.json
```
Returns:
```json
{
  "chains": [
    {
      "source": "product.created",
      "targets": ["refreshList", "showToast"]
    }
  ],
  "resolutionMode": "OneHop",
  "maxDepth": 10
}
```

### Event Resolution Preview
```
GET /_swap/dev/explain.json?event=product.created
```
Returns:
```json
{
  "event": "product.created",
  "resolvedEvents": ["refreshList", "showToast"],
  "resolutionPath": [
    { "hop": 0, "event": "product.created" },
    { "hop": 1, "event": "refreshList" },
    { "hop": 1, "event": "showToast" }
  ]
}
```

**Screenshot Example:**
```
Event Chains (Development)
---------------------------
Resolution Mode: OneHop
Total Chains: 12
Max Depth: 3

Graph:
┌─────────────────┐
│ product.created │
└────────┬────────┘
         │
    ┌────┴────┬─────────────┐
    ▼         ▼             ▼
┌──────┐  ┌────────┐  ┌──────────┐
│refresh│  │showToast│  │updateStats│
└───────┘  └─────────┘  └───────────┘
```

---

## 🧪 Testing Events

The event system is fully testable without starting a browser or parsing HTML.

### Unit Testing Event Resolution

```csharp
public class EventChainTests
{
    [Fact]
    public void ProductCreated_ResolvesToRefreshAndToast()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSwapHtmx(events =>
        {
            events.Chain(
                SwapEvents.Entity.Created("product"),
                SwapEvents.UI.RefreshList,
                SwapEvents.UI.ShowToast
            );
        });

        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IEventChainResolver>();

        // Act
        var resolved = resolver.Resolve(SwapEvents.Entity.Created("product"));

        // Assert
        Assert.Contains(SwapEvents.UI.RefreshList, resolved);
        Assert.Contains(SwapEvents.UI.ShowToast, resolved);
    }
}
```

---

### Integration Testing with Swap.Testing

```csharp
using Swap.Testing;

public class ProductTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public ProductTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task CreateProduct_EmitsEventsAndRefreshes()
    {
        // Arrange
        var form = await _client.GetAsync("/products/create");

        // Act
        var response = await form.SubmitFormAsync(new { 
            name = "Widget",
            price = 19.99
        });

        // Assert: Verify HX-Trigger header contains expected events
        response.AssertSuccess();
        await response.AssertHxTriggerAsync("productCreated", "refreshList", "showToast");
        
        // Assert: Verify DOM was updated
        await response.AssertElementContainsAsync("h2", "Widget");
    }

    [Fact]
    public async Task UpdateProduct_TriggersRefresh()
    {
        // Act
        var response = await _client.PutFormAsync("/products/1", new { 
            name = "Updated Widget" 
        });

        // Assert
        response.AssertSuccess();
        await response.AssertHxTriggerAsync("productUpdated", "refreshList");
    }
}
```

---

## 📋 Event Naming Conventions

Consistent naming makes event chains easier to understand and maintain.

### Recommended Patterns

**Entity Events:**
```csharp
product.created
product.updated
product.deleted
product.archived

order.created
order.shipped
order.completed
order.cancelled
```

**Workflow Events:**
```csharp
checkout.started
checkout.payment_received
checkout.completed

registration.started
registration.email_sent
registration.completed
```

**Data Events:**
```csharp
products.imported
orders.exported
cache.invalidated
```

**UI Events:**
```csharp
refreshList
showToast
closeModal
updateStats
clearForm
```

---

## 🛠️ CLI Tools

The Swap CLI provides tools for working with event chains.

### List All Chains

```bash
swap events list -p .
```

**Output:**
```
Event Chains:
  product.created → refreshList, showToast
  product.updated → refreshList, productPriceUpdated
  product.deleted → refreshList, closeModal
  
Total: 3 chains
```

---

### Validate Chains

```bash
swap events validate -p .
```

**Output:**
```
Validating event chains...

✅ No circular dependencies found
✅ All dependencies resolved
✅ No unreachable chains

Validation passed!
```

---

### Generate Mermaid Graph

```bash
swap events graph -p . --format mermaid
```

**Output:**
```mermaid
graph TD
    A[product.created] --> B[refreshList]
    A --> C[showToast]
    D[product.updated] --> B
    D --> E[productPriceUpdated]
    F[product.deleted] --> B
    F --> G[closeModal]
```

---

## 🎯 Real-World Examples

### Example 1: Product Management

**Chains:**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(
        SwapEvents.Entity.Created("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast,
        SwapEvents.Custom("inventory.updated")
    );

    events.Chain(
        SwapEvents.Entity.Updated("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.Custom("product.priceUpdated")
    );

    events.Chain(
        SwapEvents.Entity.Deleted("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.CloseModal
    );

    events.Chain(
        SwapEvents.Custom("product.priceUpdated"),
        SwapEvents.UI.UpdateStats
    );
});
```

**Controller:**
```csharp
public class ProductsController : SwapController
{
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), product);
        return SwapView(product);
    }

    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        await _service.UpdateAsync(id, dto);
        await _events.EmitAsync(SwapEvents.Entity.Updated("product"), new { id });
        return SwapView();
    }

    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        await _events.EmitAsync(SwapEvents.Entity.Deleted("product"), new { id });
        return Ok();
    }
}
```

**Client:**
```html
<!-- Refresh list on any product event -->
<div id="product-list" 
     hx-get="/products/list" 
     hx-trigger="refreshList from:body">
</div>

<!-- Show toast on success -->
<div hx-trigger="showToast from:body" 
     hx-swap="none" 
     hx-on::before-request="showToast('Success!')">
</div>

<!-- Update stats when price changes -->
<div id="stats" 
     hx-get="/products/stats" 
     hx-trigger="updateStats from:body">
</div>
```

---

### Example 2: Order Workflow

**Chains:**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    // Order created
    events.Chain(
        SwapEvents.Entity.Created("order"),
        SwapEvents.Custom("inventory.check"),
        SwapEvents.Custom("email.orderConfirmation"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );

    // Payment received
    events.Chain(
        SwapEvents.Custom("payment.received"),
        SwapEvents.Workflow.Completed("order"),
        SwapEvents.Custom("email.receipt"),
        SwapEvents.UI.RefreshList
    );

    // Order shipped
    events.Chain(
        SwapEvents.Custom("order.shipped"),
        SwapEvents.Custom("email.tracking"),
        SwapEvents.UI.UpdateStats
    );
});
```

**Server Event Handler (Background Worker):**
```csharp
public class InventoryModule : IModule
{
    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        registrar.Register("inventory.check", async (OrderCreated evt) =>
        {
            // Check inventory and reserve stock
            await _inventoryService.ReserveAsync(evt.ProductId, evt.Quantity);
            
            // Emit if stock is low
            if (await _inventoryService.IsLowAsync(evt.ProductId))
            {
                await _events.EmitAsync(SwapEvents.Custom("inventory.low"));
            }
        });

        registrar.Register("inventory.low", async (InventoryLow evt) =>
        {
            // Trigger reorder
            await _reorderService.CreatePurchaseOrderAsync(evt.ProductId);
        });
    }
}
```

---

## ⚙️ Configuration Reference

### AddSwapHtmx Options

```csharp
builder.Services.AddSwapHtmx(events =>
{
    // Resolution mode
    events.UseResolutionMode(ChainResolutionMode.OneHop);
    events.UseMaxTransitiveDepth(10);

    // Event chains
    events.Chain("source", "target1", "target2");

    // Server events
    events.EnableServerEvents(builder.Configuration.GetSection("Swap:ServerEvents"));
});
```

---

### Server Events Configuration

```json
{
  "Swap": {
    "ServerEvents": {
      "Enabled": true,
      "ConnectionString": "amqp://user:pass@localhost:5672",
      "ExchangeName": "swap.events",
      "QueuePrefix": "MyApp",
      "RetryCount": 3,
      "RetryDelay": "00:00:05"
    }
  }
}
```

---

## 🚨 Common Pitfalls

### 1. Circular Dependencies

**Problem:**
```csharp
events.Chain("A", "B");
events.Chain("B", "A");  // Circular!
```

**Solution:** Use validation during startup:
```bash
swap events validate -p .
```

---

### 2. Magic Strings

**Problem:**
```csharp
await _events.EmitAsync("product-created");  // Typo-prone
```

**Solution:** Use built-in constants or analyzer:
```csharp
await _events.EmitAsync(SwapEvents.Entity.Created("product"));
```

Install analyzer:
```bash
dotnet add package Swap.Htmx.Analyzers
```

---

### 3. Over-Chaining

**Problem:** Too many transitive hops make chains hard to reason about.

**Solution:** Keep chains shallow (2-3 hops max). Use domain events for orchestration.

---

### 4. Missing Event Handlers

**Problem:** Event emitted but no client-side handler defined.

**Solution:** Use dev dashboard to verify chains and ensure HTMX markup listens for events.

---

## 📚 Additional Resources

- **[PRODUCT.md](PRODUCT.md)** — Overview of Swap framework and the three pillars
- **[TEMPLATES.md](TEMPLATES.md)** — Template comparison and usage guide
- **[Wiki](https://jdtoon.github.io/swap/)** — Getting started guides and examples
- **[GitHub](https://github.com/jdtoon/swap)** — Source code and issues
- **[NuGet](https://www.nuget.org/packages?q=owner:jdtoon)** — Published packages

---

**The Swap Event System makes server-driven interactivity declarative, testable, and maintainable. Start simple with `OneHop`, expand with `Bidirectional` or `Transitive` as needed, and use server events for distributed scenarios.**
