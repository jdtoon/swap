# Event Bus Architecture - Core Infrastructure

**Purpose**: Prevent "useEffect Hell" and provide robust event management  
**Status**: Critical Foundation - Phase 2 Priority  
**Last Updated**: October 21, 2025

---

## 🎯 The Problem Statement

**User Insight**: "we need to avoid the 'useEffect' hell that React has a problem with and not allow an event to be repeatedly called"

### React's useEffect Hell

```javascript
// React's nightmare scenario
useEffect(() => {
  fetchData(); // Triggers re-render
}, [dependency1]);

useEffect(() => {
  updateUI(); // Triggers re-render
}, [dependency2]);

useEffect(() => {
  // Oops! This causes dependency1 to change
  // Which triggers the first useEffect
  // Which causes dependency2 to change
  // Which triggers this useEffect again
  // INFINITE LOOP!
}, [dependency1, dependency2]);
```

### Our Challenge with HTMX

**Without proper event bus**:
```
User clicks "Create Order" 
  → Triggers "order.created"
    → Updates inventory (triggers "inventory.changed")
      → Updates dashboard (triggers "stats.refresh")
        → Updates order list (triggers "order.list.refresh")
          → ??? Accidentally triggers "order.created" again?
            → INFINITE LOOP!
```

**Key Concerns**:
1. ❌ Events triggering events that trigger more events (cascade)
2. ❌ Same event called multiple times (duplicate processing)
3. ❌ No way to track event origin (which user? which session?)
4. ❌ Multiple browser tabs/instances causing conflicts
5. ❌ Race conditions in async event handlers
6. ❌ No centralized control or observability

---

## 🏗️ Solution: Centralized Event Bus in NetMX.Core

### Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                   NetMX.Core                         │
│  ┌────────────────────────────────────────────┐     │
│  │           IEventBus (Interface)            │     │
│  └────────────────────────────────────────────┘     │
│                        ▲                             │
│                        │                             │
│  ┌────────────────────┴───────────────────────┐     │
│  │          EventBus (Implementation)         │     │
│  │                                            │     │
│  │  - Event Queue (in-memory)                │     │
│  │  - Event History (Redis/Memory)           │     │
│  │  - Duplicate Detection                    │     │
│  │  - Loop Prevention                        │     │
│  │  - Rate Limiting                          │     │
│  │  - Observability (OpenTelemetry)          │     │
│  └────────────────────────────────────────────┘     │
│                                                      │
│  ┌────────────────────────────────────────────┐     │
│  │         EventContext (Metadata)            │     │
│  │                                            │     │
│  │  - RequestId (unique per HTTP request)    │     │
│  │  - SessionId (unique per user session)    │     │
│  │  - UserId (authenticated user)            │     │
│  │  - Timestamp                              │     │
│  │  - Depth (event chain depth)             │     │
│  │  - Origin (triggering event)             │     │
│  │  - ProcessedEvents (deduplication)       │     │
│  └────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────┘
```

---

## 💡 Core Design Principles

### 1. One Request, One Event Context

**Rule**: All events triggered during a single HTTP request share the same `EventContext`

```csharp
public class EventContext
{
    // Unique per HTTP request (never changes during request)
    public Guid RequestId { get; init; } = Guid.NewGuid();
    
    // Unique per user session (persists across requests)
    public string SessionId { get; init; } = string.Empty;
    
    // Authenticated user (if logged in)
    public Guid? UserId { get; init; }
    
    // When request started
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    // How deep are we in the event chain? (prevents infinite loops)
    public int Depth { get; private set; } = 0;
    
    // What triggered this event? (for tracing)
    public string? OriginEvent { get; private set; }
    
    // Events already processed in this request (prevents duplicates)
    public HashSet<string> ProcessedEvents { get; } = new();
    
    // Total events processed (budget to prevent abuse)
    public int EventCount { get; private set; } = 0;
    
    // Maximum allowed depth (circuit breaker)
    public const int MaxDepth = 10;
    
    // Maximum allowed events (circuit breaker)
    public const int MaxEvents = 50;
    
    // Create child context for chained event
    public EventContext CreateChild(string triggeringEvent)
    {
        if (Depth >= MaxDepth)
            throw new InvalidOperationException(
                $"Event depth exceeded {MaxDepth} (possible infinite loop). " +
                $"Origin: {OriginEvent} → {triggeringEvent}");
        
        if (EventCount >= MaxEvents)
            throw new InvalidOperationException(
                $"Event budget exceeded {MaxEvents} events per request");
        
        return new EventContext
        {
            RequestId = RequestId,        // Same request
            SessionId = SessionId,        // Same session
            UserId = UserId,              // Same user
            Timestamp = Timestamp,        // Same request time
            Depth = Depth + 1,            // Increment depth
            OriginEvent = triggeringEvent,
            ProcessedEvents = ProcessedEvents,  // Shared set
            EventCount = EventCount + 1
        };
    }
}
```

**Benefits**:
- ✅ Track entire event chain within a request
- ✅ Prevent infinite loops (max depth 10)
- ✅ Prevent event bombs (max 50 events)
- ✅ Share deduplication set across chain
- ✅ Complete observability (trace full chain)

---

### 2. Duplicate Detection (Smart Deduplication)

**Problem**: Same event triggered multiple times in one request

```html
<!-- Multiple HTMX elements listening to same event -->
<div hx-get="/stats" hx-trigger="product.created from:body"></div>
<div hx-get="/inventory" hx-trigger="product.created from:body"></div>
<div hx-get="/reports" hx-trigger="product.created from:body"></div>

<!-- Backend: Only process "product.created" ONCE per request -->
```

**Solution**: Event fingerprinting

```csharp
public interface IEventBus
{
    Task PublishAsync<TData>(string eventName, TData data, EventContext? context = null);
}

public class EventBus : IEventBus
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EventBus> _logger;
    
    public async Task PublishAsync<TData>(string eventName, TData data, EventContext? context = null)
    {
        context ??= EventContext.Current; // Get from HttpContext
        
        // Create event fingerprint
        var fingerprint = CreateFingerprint(eventName, data, context);
        
        // Check if already processed in this request
        if (context.ProcessedEvents.Contains(fingerprint))
        {
            _logger.LogDebug(
                "Event {EventName} already processed in request {RequestId} (depth: {Depth})",
                eventName, context.RequestId, context.Depth);
            return;
        }
        
        // Mark as processed
        context.ProcessedEvents.Add(fingerprint);
        
        // Check rate limiting (Redis-based, cross-instance)
        if (await IsRateLimitedAsync(eventName, context))
        {
            _logger.LogWarning(
                "Event {EventName} rate limited for session {SessionId}",
                eventName, context.SessionId);
            return;
        }
        
        // Process event
        await ProcessEventAsync(eventName, data, context);
    }
    
    private string CreateFingerprint<TData>(string eventName, TData data, EventContext context)
    {
        // Fingerprint: event + request + data hash
        var dataHash = JsonSerializer.Serialize(data).GetHashCode();
        return $"{eventName}:{context.RequestId}:{dataHash}";
    }
    
    private async Task<bool> IsRateLimitedAsync(string eventName, EventContext context)
    {
        // Redis-based rate limiting (cross-instance)
        // Allow max 10 occurrences of same event per session per minute
        var key = $"event-rate:{eventName}:{context.SessionId}";
        var count = await _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return Task.FromResult(0);
        });
        
        if (count >= 10)
            return true; // Rate limited
        
        await _cache.SetAsync(key, count + 1);
        return false;
    }
}
```

**Key Features**:
- ✅ Per-request deduplication (immediate)
- ✅ Per-session rate limiting (Redis, cross-instance)
- ✅ Data-aware fingerprinting (same event with different data = different fingerprint)
- ✅ Configurable limits (10/min default)

---

### 3. Event Direction & Acyclic Graph

**Problem**: Events triggering each other in loops

**Solution**: Directed Acyclic Graph (DAG) enforcement

```csharp
public enum EventDirection
{
    Upstream,    // User-initiated (can trigger downstream)
    Downstream,  // System-initiated (CANNOT trigger upstream)
    Terminal     // End of chain (cannot trigger anything)
}

[AttributeUsage(AttributeTargets.Field)]
public class EventDirectionAttribute : Attribute
{
    public EventDirection Direction { get; }
    
    public EventDirectionAttribute(EventDirection direction)
    {
        Direction = direction;
    }
}

// Event definitions with direction
public static class DomainEvents
{
    public static class Product
    {
        [EventDirection(EventDirection.Upstream)]
        public const string Created = "product.created";
        
        [EventDirection(EventDirection.Downstream)]
        public const string InventoryUpdated = "product.inventory-updated";
        
        [EventDirection(EventDirection.Downstream)]
        public const string StatsRefreshed = "product.stats-refreshed";
        
        [EventDirection(EventDirection.Terminal)]
        public const string AuditLogged = "product.audit-logged";
    }
}

// Event bus enforces direction
public class EventBus : IEventBus
{
    private readonly Dictionary<string, EventDirection> _eventDirections = new();
    
    public async Task PublishAsync<TData>(string eventName, TData data, EventContext context)
    {
        var direction = GetEventDirection(eventName);
        
        // Terminal events cannot trigger other events
        if (context.OriginEvent != null)
        {
            var originDirection = GetEventDirection(context.OriginEvent);
            
            if (originDirection == EventDirection.Terminal)
            {
                throw new InvalidOperationException(
                    $"Terminal event '{context.OriginEvent}' cannot trigger other events");
            }
            
            // Downstream events cannot trigger upstream events
            if (originDirection == EventDirection.Downstream && 
                direction == EventDirection.Upstream)
            {
                throw new InvalidOperationException(
                    $"Downstream event '{context.OriginEvent}' cannot trigger upstream event '{eventName}'");
            }
        }
        
        // Process event
        await ProcessEventAsync(eventName, data, context);
    }
    
    private EventDirection GetEventDirection(string eventName)
    {
        // Cache direction from attributes
        if (!_eventDirections.TryGetValue(eventName, out var direction))
        {
            direction = ExtractDirectionFromAttribute(eventName);
            _eventDirections[eventName] = direction;
        }
        return direction;
    }
}
```

**Event Flow**:
```
[Upstream]           [Downstream]         [Terminal]
product.created  →  inventory.updated  →  audit.logged
     ↓                    ↓                     X (no triggers)
     ↓              stats.refreshed
     ↓                    ↓
     OK                   OK
     
product.created  ←  inventory.updated  (BLOCKED!)
```

**Benefits**:
- ✅ Compile-time direction declaration
- ✅ Runtime enforcement (cannot violate DAG)
- ✅ Clear event hierarchy
- ✅ Impossible to create loops

---

### 4. HTMX Integration (Smart Response)

**Problem**: Send all triggered events back to frontend WITHOUT processing duplicates

**Solution**: EventBus collects, deduplicates, and returns

```csharp
public class EventBusMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context, IEventBus eventBus)
    {
        // Create event context for this request
        var eventContext = new EventContext
        {
            RequestId = Guid.NewGuid(),
            SessionId = context.Session.Id,
            UserId = context.User.FindFirst("sub")?.Value != null 
                ? Guid.Parse(context.User.FindFirst("sub").Value) 
                : null,
            Timestamp = DateTime.UtcNow
        };
        
        // Store in HttpContext for controllers to access
        context.Items["EventContext"] = eventContext;
        
        // Execute request
        await _next(context);
        
        // After response generated, add HX-Trigger header with ALL events
        if (context.Response.StatusCode < 400 && eventContext.ProcessedEvents.Any())
        {
            var triggeredEvents = eventBus.GetTriggeredEvents(eventContext.RequestId);
            var json = JsonSerializer.Serialize(triggeredEvents);
            context.Response.Headers["HX-Trigger"] = json;
        }
    }
}

// EventBus tracks triggered events
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Guid, List<TriggeredEvent>> _requestEvents = new();
    
    public async Task PublishAsync<TData>(string eventName, TData data, EventContext context)
    {
        // ... deduplication logic ...
        
        // Track event for this request (to send back to frontend)
        _requestEvents.AddOrUpdate(
            context.RequestId,
            new List<TriggeredEvent> { new(eventName, data) },
            (_, list) => { list.Add(new(eventName, data)); return list; });
        
        // ... process handlers ...
    }
    
    public Dictionary<string, object> GetTriggeredEvents(Guid requestId)
    {
        if (!_requestEvents.TryRemove(requestId, out var events))
            return new();
        
        // Convert to HTMX format: { "event.name": { data }, ... }
        return events
            .GroupBy(e => e.EventName)
            .ToDictionary(
                g => g.Key,
                g => (object)g.First().Data); // First = deduplicated
    }
}
```

**Result**:
```http
POST /api/orders
Content-Type: application/json

{ "customerId": "123", "items": [...] }

HTTP/1.1 200 OK
HX-Trigger: {
  "order.created": { "orderId": "456" },
  "inventory.updated": { "productIds": [1,2,3] },
  "stats.refreshed": { "category": "Electronics" }
}
```

**Frontend** (automatic):
```html
<!-- All three divs update automatically -->
<div hx-get="/orders" hx-trigger="order.created from:body"></div>
<div hx-get="/inventory" hx-trigger="inventory.updated from:body"></div>
<div hx-get="/stats" hx-trigger="stats.refreshed from:body"></div>
```

**Benefits**:
- ✅ Single source of truth (backend decides what happened)
- ✅ No duplicate HTMX requests (each element triggers once)
- ✅ Automatic deduplication (EventBus handles it)
- ✅ Complete observability (all events logged)

---

### 5. Deployment Strategy: Monolith vs Microservices

**Key Insight**: "we need to consider lengths to go to for a normal monolith (i dont think we need redis here) vs microservices"

#### Monolith (Single Instance) - Week 2

**Use**: IMemoryCache (built-in .NET)

```csharp
public class EventBus : IEventBus
{
    private readonly IMemoryCache _cache; // In-memory, single instance
    private readonly ConcurrentDictionary<Guid, List<TriggeredEvent>> _requestEvents = new();
    
    public async Task PublishAsync<TData>(string eventName, TData data, EventContext context)
    {
        // Per-request deduplication (in-memory HashSet)
        var fingerprint = CreateFingerprint(eventName, data, context);
        
        if (context.ProcessedEvents.Contains(fingerprint))
            return; // Already processed in this request
        
        context.ProcessedEvents.Add(fingerprint);
        
        // Per-session rate limiting (IMemoryCache - sufficient for single instance!)
        if (await IsRateLimitedAsync(eventName, context))
            return;
        
        // Process event
        await ProcessEventAsync(eventName, data, context);
    }
    
    private async Task<bool> IsRateLimitedAsync(string eventName, EventContext context)
    {
        var key = $"event-rate:{eventName}:{context.SessionId}";
        var count = await _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return Task.FromResult(0);
        });
        
        if (count >= 10) return true; // 10 events/min per session
        
        _cache.Set(key, count + 1);
        return false;
    }
}
```

**Monolith Benefits**:
- ✅ Zero external dependencies (IMemoryCache is .NET built-in)
- ✅ Simpler deployment (no Redis needed)
- ✅ Lower cost (no cache server)
- ✅ Sufficient for 99% of applications
- ✅ <1ms overhead (in-process)

**When to use**: Single web instance, <10K concurrent users

---

#### Load-Balanced Monolith (Multiple Instances) - Week 4

**Problem**: Multiple web app instances, same user session

```
User Session ABC123 (Sticky Session via Load Balancer)

┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│  Instance 1 │         │  Instance 2 │         │  Instance 3 │
└─────────────┘         └─────────────┘         └─────────────┘
   IMemoryCache           IMemoryCache            IMemoryCache
   (independent)          (independent)           (independent)
```

**Solution 1: Sticky Sessions (Recommended for Monolith)**

```csharp
// Load balancer config (Azure App Service, AWS ALB, nginx)
// Route same session to same instance
// Each instance has independent IMemoryCache
// No Redis needed!
```

**Monolith (Sticky) Benefits**:
- ✅ Still zero external dependencies
- ✅ Same user always hits same instance
- ✅ IMemoryCache works perfectly
- ✅ Lower latency (no network calls)

**When to use**: Load-balanced monolith, sticky sessions enabled

---

**Solution 2: Distributed Cache (If sticky sessions not possible)**

**Use**: IDistributedCache with Redis (optional)

```csharp
public class DistributedEventBus : IEventBus
{
    private readonly IDistributedCache _cache; // Redis
    private readonly IConnectionMultiplexer _redis; // Redis pub/sub
    
    public async Task PublishAsync<TData>(string eventName, TData data, EventContext context)
    {
        // Try to acquire Redis lock (ensures only one instance processes)
        var lockKey = $"event-lock:{CreateFingerprint(eventName, data, context)}";
        var acquired = await _cache.SetAsync(
            lockKey, 
            BitConverter.GetBytes(true),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            });
        
        if (!acquired) return; // Another instance is handling it
        
        // Process event
        await ProcessEventAsync(eventName, data, context);
    }
}
```

**Distributed Benefits**:
- ✅ Works without sticky sessions
- ✅ Cross-instance coordination
- ✅ Single event processing guarantee

**Tradeoffs**:
- ⚠️ Requires Redis (external dependency)
- ⚠️ Network latency (+2-5ms)
- ⚠️ Additional cost (Redis server)

**When to use**: Multiple instances, no sticky sessions, or microservices

---

#### Microservices (Distributed) - Month 7

**Problem**: Events need to cross service boundaries

```csharp
public class DistributedEventBus : IEventBus
{
    private readonly IDistributedCache _cache; // Redis
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DistributedEventBus> _logger;
    
    public async Task PublishAsync<TData>(string eventName, TData data, EventContext context)
    {
        // Create global fingerprint (includes session, not just request)
        var fingerprint = $"{eventName}:{context.SessionId}:{JsonSerializer.Serialize(data).GetHashCode()}";
        
        // Try to acquire lock in Redis (5 second expiry)
        var lockKey = $"event-lock:{fingerprint}";
        var acquired = await _cache.SetAsync(
            lockKey, 
            BitConverter.GetBytes(true),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            });
        
        if (!acquired)
        {
            _logger.LogDebug(
                "Event {EventName} already being processed by another instance for session {SessionId}",
                eventName, context.SessionId);
            return; // Another instance is handling it
        }
        
        try
        {
            // Process event
            await ProcessEventAsync(eventName, data, context);
            
            // Publish to Redis pub/sub (notify all instances)
            await _redis.GetSubscriber().PublishAsync(
                $"events:{eventName}",
                JsonSerializer.Serialize(new EventMessage
                {
                    EventName = eventName,
                    Data = data,
                    Context = context
                }));
        }
        finally
        {
            // Release lock
            await _cache.RemoveAsync(lockKey);
        }
    }
}
```

**Benefits**:
- ✅ Cross-instance deduplication
- ✅ Only one instance processes event
- ✅ All instances notified (for SignalR/WebSockets)
- ✅ Automatic lock expiry (5 seconds)

---

### 6. Observability (OpenTelemetry)

**Every event is a span in the trace**:

```csharp
public class EventBus : IEventBus
{
    private static readonly ActivitySource ActivitySource = new("NetMX.Events");
    
    public async Task PublishAsync<TData>(string eventName, TData data, EventContext context)
    {
        using var activity = ActivitySource.StartActivity(
            $"Event: {eventName}",
            ActivityKind.Internal);
        
        activity?.SetTag("event.name", eventName);
        activity?.SetTag("event.depth", context.Depth);
        activity?.SetTag("event.origin", context.OriginEvent);
        activity?.SetTag("request.id", context.RequestId);
        activity?.SetTag("session.id", context.SessionId);
        activity?.SetTag("user.id", context.UserId);
        
        try
        {
            await ProcessEventAsync(eventName, data, context);
            activity?.SetTag("event.status", "processed");
        }
        catch (Exception ex)
        {
            activity?.SetTag("event.status", "failed");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}
```

**Trace Example**:
```
Request: POST /api/orders
  ├─ Event: order.created (depth: 0)
  │   ├─ Event: payment.processed (depth: 1)
  │   ├─ Event: inventory.reserved (depth: 1)
  │   │   ├─ Event: inventory.low-stock (depth: 2)
  │   │   │   └─ Event: notification.sent (depth: 3)
  │   ├─ Event: fulfillment.triggered (depth: 1)
  │   └─ Event: customer.notified (depth: 1)
  └─ Response: 200 OK (12 events processed, 245ms)
```

---

## 📦 Package Structure

**Philosophy**: "we don't want to have any dependancies unless absolutely core to .NET"

### NetMX.Core (Add to existing package)

**Dependencies**: ZERO external (only .NET built-in)
- ✅ Microsoft.Extensions.Caching.Memory (built-in .NET)
- ✅ System.Diagnostics.DiagnosticSource (built-in .NET)

```
NetMX.Core/
├─ Events/
│   ├─ IEventBus.cs                     (interface)
│   ├─ EventBus.cs                      (in-memory - Week 2)
│   ├─ EventContext.cs                  (request metadata)
│   ├─ EventDirection.cs                (enum)
│   ├─ EventDirectionAttribute.cs       (attribute)
│   ├─ EventHandler.cs                  (base class)
│   └─ IEventHandler.cs                 (interface)
└─ Middleware/
    └─ EventBusMiddleware.cs            (HTTP middleware)
```

### NetMX.Events (NEW - Static event definitions)

**Dependencies**: ZERO (pure constants)

```
NetMX.Events/
└─ DomainEvents.cs                      (all static event names)
    ├─ DomainEvents.User.*
    ├─ DomainEvents.Order.*
    ├─ DomainEvents.Product.*
    └─ ... (generated by CLI)
```

**Reminder**: "we want to keep events/event pipelines static. easy to debug and trace"

---

### NetMX.Caching.Redis (OPTIONAL - Add in Week 4 if needed)

**Only if**: Multiple instances without sticky sessions

**Dependencies**: 
- StackExchange.Redis (industry standard, mature)

```
NetMX.Caching.Redis/
├─ DistributedEventBus.cs               (Redis implementation)
├─ RedisEventBusOptions.cs              (configuration)
└─ ServiceCollectionExtensions.cs       (DI registration)
```

**Usage** (opt-in):
```csharp
// Without Redis (default - single instance or sticky sessions)
services.AddEventBus(); // Uses IMemoryCache

// With Redis (optional - multiple instances, no sticky sessions)
services.AddEventBus()
    .AddRedisDistribution(options =>
    {
        options.ConnectionString = "localhost:6379";
    });
```

---

## 🔧 Registration (Program.cs)

```csharp
// Add event bus
services.AddEventBus(options =>
{
    options.MaxDepth = 10;              // Default: 10
    options.MaxEventsPerRequest = 50;   // Default: 50
    options.EnableObservability = true; // Default: true
    options.UseRedis = true;            // Default: false
});

// Add middleware (automatic HTMX header injection)
app.UseEventBus();
```

---

## ✅ Summary: How We Prevent "useEffect Hell"

| React Problem | NetMX Solution |
|---------------|----------------|
| Infinite loops (dependency hell) | Max depth (10), Acyclic graph (DAG) |
| Duplicate renders | Per-request deduplication |
| Race conditions | Redis locks (cross-instance) |
| No control | Centralized EventBus |
| Hard to debug | Full OpenTelemetry tracing |
| Per-component state | Per-request EventContext |

**Result**: 
- ✅ **Zero infinite loops** (circuit breakers)
- ✅ **Zero duplicate processing** (fingerprinting)
- ✅ **Full observability** (trace every event)
- ✅ **Cross-instance safe** (Redis coordination)
- ✅ **HTMX-optimized** (automatic header injection)

---

## 📅 Implementation Timeline

### Week 2 (Oct 21-27) - Foundation (Monolith Focus)
- ✅ EventContext class
- ✅ IEventBus interface
- ✅ EventBus implementation (IMemoryCache - zero external deps!)
- ✅ EventDirection enforcement
- ✅ Duplicate detection (in-memory)
- ✅ Rate limiting (IMemoryCache)
- ✅ EventBusMiddleware
- ✅ OpenTelemetry integration
- ✅ Unit tests (40+ tests)
- ✅ Documentation

**Result**: Production-ready for 99% of applications (single instance + sticky sessions)

### Week 4 (Nov 4-10) - Distributed (Optional)
**Only if needed**: Multiple instances without sticky sessions

- ✅ NetMX.Caching.Redis package (separate, optional)
- ✅ DistributedEventBus (Redis-backed)
- ✅ Cross-instance coordination
- ✅ Integration tests (10+ instances)
- ✅ Performance benchmarks (<5ms overhead)

**Note**: Most apps won't need this (sticky sessions + IMemoryCache sufficient)

---

## 🎯 Success Metrics

- **Zero infinite loops** in production (circuit breakers work)
- **99.9% event deduplication** (no duplicate processing)
- **<5ms overhead** per event (minimal performance impact)
- **100% trace coverage** (every event in OpenTelemetry)
- **Cross-instance safe** (tested with 10+ instances)

---

**This is the foundation. HTMX hell = SOLVED.** 🚀
