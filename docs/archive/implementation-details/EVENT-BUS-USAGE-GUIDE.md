# Event Bus + HTMX Integration - Complete Guide

**Date**: October 21, 2025  
**Status**: ✅ **COMPLETE** - Production Ready!

---

## 🎉 Overview

The NetMX Event Bus provides **type-safe, loop-free event handling** with **automatic HTMX integration**. Events published from controllers automatically trigger HTMX listeners - zero manual work!

### Key Features
- ✅ **Zero infinite loops** - Max depth 10, circuit breakers
- ✅ **Automatic deduplication** - SHA256 fingerprinting
- ✅ **HTMX integration** - Auto-inject HX-Trigger headers
- ✅ **OpenTelemetry observability** - Trace every event
- ✅ **Rate limiting** - 10 events/min per session
- ✅ **Event direction enforcement** - DAG prevents backward triggers

---

## 📦 Setup (3 Steps)

### Step 1: Register Event Bus

In `Program.cs`:

```csharp
using NetMX.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Event Bus services
builder.Services.AddEventBus();

// Register your event handlers
builder.Services.AddEventHandler<ProductCreatedHandler, ProductCreatedData>();
builder.Services.AddEventHandler<InventoryUpdatedHandler, InventoryData>();

var app = builder.Build();

// Use Event Bus middleware (CRITICAL - must be before UseEndpoints)
app.UseRouting();
app.UseEventBus();  // <-- Add this line
app.MapControllers();

app.Run();
```

### Step 2: Define Type-Safe Events

In `Events/DomainEvents.cs`:

```csharp
using NetMX.Events;

namespace MyApp.Events;

public static class DomainEvents
{
    public static class Product
    {
        [EventDirection(EventDirection.Upstream)]
        public const string Created = "product.created";
        
        [EventDirection(EventDirection.Upstream)]
        public const string Updated = "product.updated";
        
        [EventDirection(EventDirection.Upstream)]
        public const string Deleted = "product.deleted";
    }
    
    public static class Inventory
    {
        [EventDirection(EventDirection.Downstream)]
        public const string Updated = "inventory.updated";
        
        [EventDirection(EventDirection.Downstream)]
        public const string LowStock = "inventory.low-stock";
    }
    
    public static class Audit
    {
        [EventDirection(EventDirection.Terminal)]
        public const string Logged = "audit.logged";
    }
}
```

**Event Direction Rules**:
- **Upstream**: User-initiated actions (button clicks, form submits)
- **Downstream**: System reactions (inventory updates, notifications)
- **Terminal**: End-of-chain (audit logs, emails sent)

**DAG Enforcement**:
- Upstream → Downstream ✅
- Downstream → Downstream ✅
- Downstream → Terminal ✅
- Downstream → Upstream ❌ (BLOCKED!)
- Terminal → Anything ❌ (BLOCKED!)

### Step 3: Inject EventBus in Controllers

```csharp
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using MyApp.Events;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly IEventBus _eventBus;  // <-- Inject here
    
    public ProductsController(IProductService service, IEventBus eventBus)
    {
        _service = service;
        _eventBus = eventBus;
    }
    
    // ... methods below
}
```

---

## 🚀 Publishing Events (3 Patterns)

### Pattern 1: Simple Event (Most Common)

```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    var product = await _service.CreateAsync(dto);
    
    // Publish event - will auto-inject HX-Trigger header
    await this.PublishEventAsync(_eventBus, 
        DomainEvents.Product.Created, 
        new { productId = product.Id, name = product.Name });
    
    return Ok();
}
```

**Result**: Response includes `HX-Trigger: {"product.created": {"productId": 123, "name": "Widget"}}`

### Pattern 2: Multiple Events

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> Update(int id, UpdateProductDto dto)
{
    var product = await _service.UpdateAsync(id, dto);
    
    // Trigger multiple events
    await this.PublishEventAsync(_eventBus, 
        DomainEvents.Product.Updated, 
        new { productId = id });
        
    await this.PublishEventAsync(_eventBus, 
        DomainEvents.Inventory.Updated, 
        new { productId = id, stock = product.Stock });
    
    return Ok();
}
```

**Result**: `HX-Trigger: {"product.updated": {...}, "inventory.updated": {...}}`

### Pattern 3: Cascading Events (Advanced)

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    await _service.DeleteAsync(id);
    
    // Trigger upstream event
    await this.PublishEventAsync(_eventBus, 
        DomainEvents.Product.Deleted, 
        new { productId = id });
    
    // This will trigger downstream handlers (inventory, audit, etc.)
    // Each handler can trigger more downstream events
    // EventContext prevents infinite loops!
    
    return Ok();
}
```

---

## 🎧 Creating Event Handlers

### Simple Handler

```csharp
using NetMX.Events;
using MyApp.Events;

public class ProductCreatedHandler : IEventHandler<ProductCreatedData>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ProductCreatedHandler> _logger;
    
    public ProductCreatedHandler(
        IInventoryService inventoryService,
        ILogger<ProductCreatedHandler> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }
    
    public async Task HandleAsync(
        string eventName,
        ProductCreatedData data,
        EventContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Product {ProductId} created. Initializing inventory.",
            data.ProductId);
        
        // React to event
        await _inventoryService.CreateInventoryRecordAsync(
            data.ProductId, 
            initialStock: 0, 
            cancellationToken);
    }
}

public record ProductCreatedData(int ProductId, string Name);
```

### Cascading Handler (Triggers More Events)

```csharp
public class InventoryUpdatedHandler : IEventHandler<InventoryData>
{
    private readonly IEventBus _eventBus;
    private readonly INotificationService _notificationService;
    
    public InventoryUpdatedHandler(
        IEventBus eventBus,
        INotificationService notificationService)
    {
        _eventBus = eventBus;
        _notificationService = notificationService;
    }
    
    public async Task HandleAsync(
        string eventName,
        InventoryData data,
        EventContext context,
        CancellationToken cancellationToken = default)
    {
        // Check for low stock
        if (data.Stock < 10)
        {
            // Trigger downstream event (creates child EventContext)
            var childContext = context.CreateChild(DomainEvents.Inventory.Updated);
            
            await _eventBus.PublishAsync(
                DomainEvents.Inventory.LowStock,
                new { productId = data.ProductId, stock = data.Stock },
                childContext,
                cancellationToken);
        }
        
        // Send notification (Terminal event - won't trigger more events)
        var terminalContext = context.CreateChild(DomainEvents.Inventory.Updated);
        
        await _eventBus.PublishAsync(
            DomainEvents.Audit.Logged,
            new { action = "InventoryUpdated", productId = data.ProductId },
            terminalContext,
            cancellationToken);
    }
}

public record InventoryData(int ProductId, int Stock);
```

---

## 🎨 HTMX Integration (Frontend)

### Auto-Refresh List on Create

```html
<!-- Products list container -->
<div id="product-list" 
     hx-get="/api/products/list" 
     hx-trigger="load, product.created from:body"
     hx-swap="innerHTML">
    <!-- List renders here -->
</div>

<!-- Create button -->
<button hx-post="/api/products"
        hx-include="#product-form"
        hx-target="#product-list">
    Create Product
</button>
```

**Flow**:
1. User clicks "Create Product"
2. POST `/api/products` → Controller publishes `product.created` event
3. EventBusMiddleware injects `HX-Trigger: product.created`
4. HTMX detects event → fires `hx-trigger="product.created from:body"`
5. GET `/api/products/list` → refreshes list automatically!

### Multi-Component Coordination

```html
<!-- Product list -->
<div id="product-list" 
     hx-get="/products/list"
     hx-trigger="product.created from:body, product.updated from:body">
</div>

<!-- Inventory panel -->
<div id="inventory-panel"
     hx-get="/inventory/summary"
     hx-trigger="inventory.updated from:body">
</div>

<!-- Stats widget -->
<div id="stats"
     hx-get="/stats"
     hx-trigger="product.created from:body, inventory.low-stock from:body">
</div>
```

**Flow**:
1. User creates product
2. Controller publishes `product.created`
3. ProductCreatedHandler triggers `inventory.updated`
4. InventoryUpdatedHandler checks stock → triggers `inventory.low-stock`
5. EventBusMiddleware injects ALL events
6. HTMX updates ALL listening components simultaneously!

### Event with Payload

```html
<div hx-get="/products/details"
     hx-trigger="product.created from:body"
     hx-vals="js:{productId: event.detail.productId}">
</div>

<script>
document.body.addEventListener('product.created', (event) => {
    console.log('Product created:', event.detail.productId);
    // Can also do custom JS if needed
});
</script>
```

---

## 🔍 Observability

### OpenTelemetry Tracing

Every event creates spans:

```
Activity: EventBus.Publish
  Tags:
    event.name: product.created
    event.depth: 0
    request.id: abc123
    session.id: xyz789
    handlers.count: 2
    duration.ms: 15
    result: success
  
  Child Activity: EventHandler.Execute
    Tags:
      handler.type: ProductCreatedHandler
      event.name: product.created
      result: success
```

### Logs

```csharp
// Automatic logging at DEBUG level
_logger.LogDebug(
    "Published event {EventName} with {HandlerCount} handlers in {Duration}ms.",
    "product.created", 2, 15.2);

// Loop prevention logged at WARNING level
_logger.LogWarning(
    "Event depth exceeded {MaxDepth} for event {EventName}. Origin: {Origin}. Stopping propagation.",
    10, "product.updated", "product.created");

// Rate limiting logged at WARNING level
_logger.LogWarning(
    "Rate limit exceeded for event {EventName} in session {SessionId}.",
    "product.created", "xyz789");
```

### Metrics

Access via ILogger or OpenTelemetry:
- Event count per request
- Event depth distribution
- Handler execution time
- Deduplication hit rate
- Rate limit violations

---

## ⚠️ Best Practices

### ✅ DO

1. **Use type-safe event constants**
   ```csharp
   await this.PublishEventAsync(_eventBus, DomainEvents.Product.Created, data);
   ```

2. **Set proper EventDirection**
   ```csharp
   [EventDirection(EventDirection.Upstream)]
   public const string Created = "product.created";
   ```

3. **Keep event data small**
   ```csharp
   new { productId = 123, name = "Widget" }  // Good
   ```

4. **Use multiple events for multiple concerns**
   ```csharp
   await PublishEventAsync(..., DomainEvents.Product.Created, ...);
   await PublishEventAsync(..., DomainEvents.Audit.Logged, ...);
   ```

5. **Inject IEventBus in constructors**
   ```csharp
   public ProductsController(IEventBus eventBus) { }
   ```

### ❌ DON'T

1. **Don't use magic strings**
   ```csharp
   await PublishEventAsync(..., "product-created", ...);  // BAD!
   ```

2. **Don't send entire entities**
   ```csharp
   new { product = entireProductObject }  // BAD! Send IDs only
   ```

3. **Don't create circular dependencies**
   ```csharp
   // BAD: product.created → inventory.updated → product.updated → ...
   // Use EventDirection to prevent this!
   ```

4. **Don't forget to register handlers**
   ```csharp
   services.AddEventHandler<MyHandler, MyData>();  // Required!
   ```

5. **Don't use EventBus for synchronous logic**
   ```csharp
   // BAD: Using events for required business logic
   await PublishEventAsync(...);  // Handler MUST succeed? Use direct call!
   
   // GOOD: Using events for side effects
   await PublishEventAsync(...);  // Notification, audit, etc. - failure is OK
   ```

---

## 🧪 Testing

### Unit Test Controller

```csharp
[Fact]
public async Task Create_ShouldPublishEvent()
{
    // Arrange
    var eventBusMock = new Mock<IEventBus>();
    var controller = new ProductsController(_service, eventBusMock.Object);
    
    // Act
    await controller.Create(new CreateProductDto { Name = "Widget" });
    
    // Assert
    eventBusMock.Verify(
        e => e.PublishAsync(
            DomainEvents.Product.Created,
            It.IsAny<object>(),
            It.IsAny<EventContext>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Integration Test with HTMX

```csharp
[Fact]
public async Task Create_ShouldInjectHxTriggerHeader()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.PostAsync("/api/products", content);
    
    // Assert
    response.Headers.Should().Contain("HX-Trigger");
    var header = response.Headers.GetValues("HX-Trigger").First();
    header.Should().Contain("product.created");
}
```

---

## 🎯 Common Patterns

### Pattern: Auto-Refresh on CRUD

```html
<!-- List container -->
<div id="list" 
     hx-get="/products/list"
     hx-trigger="load, product.created from:body, product.updated from:body, product.deleted from:body">
</div>
```

### Pattern: Show Toast on Event

```html
<div id="toast-container" 
     hx-trigger="product.created from:body"
     hx-post="/toast/success"
     hx-vals='{"message": "Product created!"}'>
</div>
```

### Pattern: Update Multiple Sections

```html
<div id="product-list" hx-trigger="product.created from:body"></div>
<div id="stats" hx-trigger="product.created from:body"></div>
<div id="recent-activity" hx-trigger="product.created from:body"></div>
```

### Pattern: Conditional Updates

```html
<div hx-get="/products/list"
     hx-trigger="product.created[event.detail.categoryId == 5] from:body">
</div>
```

---

## 📚 API Reference

### IEventBus

```csharp
public interface IEventBus
{
    Task PublishAsync<TData>(
        string eventName, 
        TData data, 
        EventContext? context = null, 
        CancellationToken cancellationToken = default);
        
    Dictionary<string, object> GetTriggeredEvents(Guid requestId);
}
```

### EventContext

```csharp
public class EventContext
{
    public Guid RequestId { get; init; }
    public string SessionId { get; init; }
    public Guid? UserId { get; init; }
    public DateTime Timestamp { get; init; }
    
    public int Depth { get; private set; }
    public string? OriginEvent { get; private set; }
    public HashSet<string> ProcessedEvents { get; }
    public int EventCount { get; private set; }
    
    public EventContext CreateChild(string triggeringEvent);
    public void IncrementEventCount();
}
```

### Controller Extensions

```csharp
public static async Task PublishEventAsync<TData>(
    this ControllerBase controller,
    IEventBus eventBus,
    string eventName,
    TData data,
    CancellationToken cancellationToken = default);

public static EventContext GetEventContext(this ControllerBase controller);
```

---

## 🔧 Troubleshooting

### Events Not Triggering in HTMX

**Problem**: HTMX listeners not firing

**Check**:
1. Is EventBusMiddleware registered? `app.UseEventBus()`
2. Is EventBus service registered? `services.AddEventBus()`
3. Are events being published? Check logs
4. Is HTMX listening? Check `hx-trigger` attribute
5. Are event names correct? Use type-safe constants

### Infinite Loop Detected

**Problem**: "Event depth exceeded 10"

**Solution**: Event direction is wrong. Use `EventDirection.Terminal` for end-of-chain events.

### Events Not Deduplicating

**Problem**: Same event processed multiple times

**Check**: EventContext is being reused across event publishes (use `CreateChild()`)

### Rate Limit Exceeded

**Problem**: "Rate limit exceeded for event X"

**Solution**: Reduce event frequency or increase limit (default: 10/min per session)

---

## 🎉 Success!

You now have a fully functional Event Bus with HTMX integration!

**Next Steps**:
1. Define your domain events in `Events/DomainEvents.cs`
2. Create event handlers for business logic
3. Publish events from controllers
4. Add HTMX triggers in views
5. Monitor with OpenTelemetry

**Questions?** Check the tests in `NetMX.AspNetCore.Core.Tests/Events/` for examples.

---

**Last Updated**: October 21, 2025  
**Status**: Production Ready ✅  
**Tests**: 11 passing (middleware + extensions)
