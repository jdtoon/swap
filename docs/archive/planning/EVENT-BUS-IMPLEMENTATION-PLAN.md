# Event Bus Implementation Plan

**Date**: October 22, 2025  
**Status**: Planning Phase  
**Priority**: 🔥 CRITICAL - Foundation for event-driven HTMX architecture

---

## 🎯 Overview

The Event Bus is the **backbone of NetMX's event-driven architecture**. It enables:
- HTMX components to communicate without tight coupling
- Cross-module event communication
- Real-time UI updates without page reloads
- Type-safe event handling with compile-time checking

---

## 📋 Current State

### ✅ What We Have
1. **NetMX.Events Package** - Base DomainEvents class (partial, extensible)
2. **Partial Class Pattern** - Modules can extend DomainEvents
3. **HTMX Integration** - `HxTrigger()` extension method
4. **Authorization Module** - Working example of event triggers/listeners

### ❌ What We Need
1. **Server-side Event Bus** - Coordinate events across requests
2. **Event Loop Prevention** - Stop infinite event chains
3. **Event Deduplication** - Prevent duplicate processing
4. **Cross-instance Coordination** - Handle multiple server instances
5. **Observability** - Trace every event through the system
6. **Rate Limiting** - Prevent event flooding

---

## 🏗️ Architecture Design

### Phase 1: In-Process Event Bus (Week 3) 🔥 HIGH

**Goal**: Single-server event coordination

**Components**:

1. **IEventBus Interface**
```csharp
namespace NetMX.Events;

/// <summary>
/// Coordinates domain events within a single application instance.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish event to all registered handlers
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : DomainEvent;
    
    /// <summary>
    /// Subscribe handler to event type
    /// </summary>
    void Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : DomainEvent;
}
```

2. **InMemoryEventBus Implementation**
```csharp
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ActivitySource _activitySource;
    
    private readonly ConcurrentDictionary<Type, List<Type>> _handlers = new();
    
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
    {
        using var activity = _activitySource.StartActivity("EventBus.Publish");
        activity?.SetTag("event.type", typeof(TEvent).Name);
        
        var handlers = GetHandlers<TEvent>();
        
        foreach (var handlerType in handlers)
        {
            var handler = (IEventHandler<TEvent>)_services.GetRequiredService(handlerType);
            await handler.HandleAsync(@event, ct);
        }
    }
}
```

3. **Event Handler Interface**
```csharp
public interface IEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

**Implementation Time**: 4-6 hours  
**Testing**: 2 hours  
**Documentation**: 1 hour

---

### Phase 2: Event Loop Prevention (Week 3) 🔥 HIGH

**Problem**: Events trigger other events, creating infinite loops

**Solution**: EventContext with depth tracking

```csharp
public class EventContext
{
    private static readonly AsyncLocal<EventContext?> _current = new();
    
    public static EventContext Current => _current.Value ??= new EventContext();
    
    public int Depth { get; private set; }
    public int EventCount { get; private set; }
    
    private const int MAX_DEPTH = 10;
    private const int MAX_EVENTS_PER_REQUEST = 50;
    
    public void EnterEventScope()
    {
        Depth++;
        EventCount++;
        
        if (Depth > MAX_DEPTH)
            throw new EventLoopException($"Max event depth {MAX_DEPTH} exceeded");
        
        if (EventCount > MAX_EVENTS_PER_REQUEST)
            throw new EventLoopException($"Max events {MAX_EVENTS_PER_REQUEST} exceeded");
    }
    
    public void ExitEventScope() => Depth--;
}
```

**Usage in EventBus**:
```csharp
public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
{
    EventContext.Current.EnterEventScope();
    try
    {
        // ... publish to handlers
    }
    finally
    {
        EventContext.Current.ExitEventScope();
    }
}
```

**Implementation Time**: 2-3 hours  
**Testing**: 2 hours

---

### Phase 3: Event Deduplication (Week 4) 🔥 MEDIUM

**Problem**: Same event published multiple times in one request

**Solution**: Event fingerprinting + cache

```csharp
public class EventDeduplicator
{
    private readonly IMemoryCache _cache;
    
    public bool IsDuplicate<TEvent>(TEvent @event)
    {
        var fingerprint = GenerateFingerprint(@event);
        var key = $"event:{fingerprint}";
        
        if (_cache.TryGetValue(key, out _))
            return true; // Duplicate!
        
        // Cache for duration of request (1 minute max)
        _cache.Set(key, true, TimeSpan.FromMinutes(1));
        return false;
    }
    
    private string GenerateFingerprint<TEvent>(TEvent @event)
    {
        // Hash: event type + key properties + timestamp (within 100ms)
        var json = JsonSerializer.Serialize(@event);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 100;
        return $"{typeof(TEvent).Name}:{Hash(json)}:{timestamp}";
    }
}
```

**Integration**:
```csharp
public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
{
    if (_deduplicator.IsDuplicate(@event))
    {
        _logger.LogDebug("Skipping duplicate event: {EventType}", typeof(TEvent).Name);
        return; // Skip
    }
    
    // ... normal publish
}
```

**Implementation Time**: 3-4 hours  
**Testing**: 2 hours

---

### Phase 4: Observability (Week 4) 🔥 MEDIUM

**Goal**: Trace every event through the system

**Components**:

1. **Activity Source**
```csharp
private static readonly ActivitySource _activitySource = 
    new ActivitySource("NetMX.EventBus");
```

2. **Event Tracing**
```csharp
public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
{
    using var activity = _activitySource.StartActivity("EventBus.Publish");
    activity?.SetTag("event.type", typeof(TEvent).Name);
    activity?.SetTag("event.depth", EventContext.Current.Depth);
    activity?.SetTag("event.count", EventContext.Current.EventCount);
    
    try
    {
        // ... publish
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

3. **Structured Logging**
```csharp
_logger.LogInformation(
    "Event published: {EventType}, Depth: {Depth}, Duration: {Duration}ms",
    typeof(TEvent).Name,
    EventContext.Current.Depth,
    stopwatch.ElapsedMilliseconds);
```

**Implementation Time**: 2-3 hours  
**Testing**: 1 hour

---

### Phase 5: Rate Limiting (Week 5) 🟡 LOW

**Problem**: Malicious/buggy code flooding events

**Solution**: Per-user rate limiter

```csharp
public class EventRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly ICurrentUser _currentUser;
    
    private const int MAX_EVENTS_PER_MINUTE = 100;
    
    public bool IsRateLimited()
    {
        var userId = _currentUser.Id ?? "anonymous";
        var key = $"rate:{userId}:{DateTime.UtcNow:yyyy-MM-dd-HH-mm}";
        
        var count = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });
        
        count++;
        _cache.Set(key, count);
        
        return count > MAX_EVENTS_PER_MINUTE;
    }
}
```

**Implementation Time**: 2 hours  
**Testing**: 1 hour

---

### Phase 6: Distributed Event Bus (Phase 3+) 🟢 FUTURE

**For multi-server deployments**

**Options**:
1. **Redis Pub/Sub** - Simple, fast
2. **RabbitMQ** - Reliable, feature-rich
3. **Azure Service Bus** - Enterprise-grade
4. **Kafka** - High-throughput

**Not needed for now** - Focus on single-server first

---

## 📊 Implementation Timeline

### Week 3 (Oct 28 - Nov 3)
- ✅ Phase 1: In-Process Event Bus (6 hours)
- ✅ Phase 2: Event Loop Prevention (4 hours)
- ✅ Document architecture (2 hours)
- ✅ Write comprehensive tests (4 hours)
- **Total**: ~16 hours

### Week 4 (Nov 4-10)
- ✅ Phase 3: Event Deduplication (5 hours)
- ✅ Phase 4: Observability (3 hours)
- ✅ Integration tests (3 hours)
- ✅ Performance testing (2 hours)
- **Total**: ~13 hours

### Week 5 (Nov 11-17)
- ✅ Phase 5: Rate Limiting (3 hours)
- ✅ E2E testing with Authorization module (4 hours)
- ✅ Documentation + examples (3 hours)
- **Total**: ~10 hours

**Total Effort**: ~40 hours (1 week of focused work)

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task PublishAsync_ShouldCallAllHandlers()
{
    // Arrange
    var handler1 = new Mock<IEventHandler<UserCreated>>();
    var handler2 = new Mock<IEventHandler<UserCreated>>();
    var eventBus = CreateEventBus(handler1, handler2);
    var @event = new UserCreated(Guid.NewGuid());
    
    // Act
    await eventBus.PublishAsync(@event);
    
    // Assert
    handler1.Verify(h => h.HandleAsync(@event, default), Times.Once);
    handler2.Verify(h => h.HandleAsync(@event, default), Times.Once);
}

[Fact]
public async Task PublishAsync_WithLoopDepth_ShouldThrow()
{
    // Arrange
    var eventBus = CreateEventBus();
    var context = EventContext.Current;
    for (int i = 0; i < 10; i++) context.EnterEventScope();
    
    // Act & Assert
    await Assert.ThrowsAsync<EventLoopException>(
        () => eventBus.PublishAsync(new UserCreated(Guid.NewGuid())));
}
```

### Integration Tests
```csharp
[Fact]
public async Task EventBus_WithRealHandlers_ShouldWork()
{
    // Arrange
    var services = new ServiceCollection()
        .AddLogging()
        .AddMemoryCache()
        .AddSingleton<IEventBus, InMemoryEventBus>()
        .AddSingleton<IEventHandler<UserCreated>, NotifyAdminHandler>()
        .BuildServiceProvider();
    
    var eventBus = services.GetRequiredService<IEventBus>();
    
    // Act
    await eventBus.PublishAsync(new UserCreated(Guid.NewGuid()));
    
    // Assert - check handler was called (spy pattern)
}
```

---

## 📚 Documentation

### For Module Developers
```markdown
# Using the Event Bus

## Publishing Events
```csharp
public class UserService
{
    private readonly IEventBus _eventBus;
    
    public async Task CreateUserAsync(CreateUserDto dto)
    {
        var user = new User(dto.Email);
        await _repository.InsertAsync(user);
        
        // Publish event
        await _eventBus.PublishAsync(new UserCreated(user.Id));
    }
}
```

## Subscribing to Events
```csharp
public class SendWelcomeEmailHandler : IEventHandler<UserCreated>
{
    private readonly IEmailService _emailService;
    
    public async Task HandleAsync(UserCreated @event, CancellationToken ct)
    {
        await _emailService.SendWelcomeEmailAsync(@event.UserId);
    }
}
```

## Registration
```csharp
services.AddSingleton<IEventBus, InMemoryEventBus>();
services.AddTransient<IEventHandler<UserCreated>, SendWelcomeEmailHandler>();
```
```

---

## 🎯 Success Criteria

**Event Bus is ready when**:
- ✅ Can publish/subscribe to events
- ✅ Prevents infinite event loops
- ✅ Deduplicates events within request
- ✅ Full observability (tracing, logging)
- ✅ Rate limiting prevents abuse
- ✅ 100% test coverage
- ✅ Documentation complete
- ✅ Working in Authorization module

---

## 🚨 Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Infinite loops crash app | HIGH | EventContext with max depth |
| Event flooding DoS | MEDIUM | Rate limiting per user |
| Performance degradation | MEDIUM | Async handlers, caching |
| Cross-instance sync | LOW | Phase 6 (future) |
| Event ordering issues | LOW | Sequential processing |

---

## 💡 Key Insights

### Why In-Process First?
- **Simpler** - No external dependencies
- **Faster** - No network overhead
- **Sufficient** - 90% of apps are single-server
- **Extensible** - Can add Redis later

### Why Not Just Use MediatR?
- **Too heavyweight** - We only need pub/sub
- **Not HTMX-optimized** - No loop prevention
- **Dependencies** - Adds 5+ packages
- **Our control** - Can optimize for our use case

### Why Partial Classes for Events?
- **Type-safe** - Compile-time checking
- **Discoverable** - IntelliSense shows all events
- **Extensible** - Modules add their own events
- **No coupling** - Framework doesn't depend on modules

---

## 📅 Next Steps

**Immediate** (This week):
1. ✅ Commit current work (domain events architecture)
2. ✅ Apply pattern to Identity/Audit modules
3. ✅ Test local NuGet packaging

**Week 3** (Oct 28 - Nov 3):
1. Implement Phase 1 (In-Process Event Bus)
2. Implement Phase 2 (Loop Prevention)
3. Write comprehensive tests
4. Document usage patterns

**Week 4** (Nov 4-10):
1. Implement Phase 3 (Deduplication)
2. Implement Phase 4 (Observability)
3. Performance testing
4. Integration with all modules

---

**Remember**: The Event Bus is the **foundation** of NetMX's event-driven architecture. Get this right, and everything else flows naturally! 🚀
