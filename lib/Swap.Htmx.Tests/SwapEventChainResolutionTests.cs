using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Events;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for the simplified one-hop event chain resolution.
/// Verifies that chains expand exactly one level and don't filter based on client subscriptions.
/// </summary>
public class SwapEventChainResolutionTests
{
    // Test event keys (following pattern that apps would use)
    private static class TestEvents
    {
        public static readonly EventKey EventHappened = new("event.happened");
        public static readonly EventKey DomainCreated = new("domain.created");
        public static readonly EventKey UiRefresh = new("ui.refresh");
        public static readonly EventKey UiToast = new("ui.toast");
        public static readonly EventKey UiUpdateCount = new("ui.updateCount");
        public static readonly EventKey A = new("A");
        public static readonly EventKey B = new("B");
        public static readonly EventKey C = new("C");
        public static readonly EventKey EventA = new("event.a");
        public static readonly EventKey EventB = new("event.b");
        public static readonly EventKey UiX = new("ui.x");
        public static readonly EventKey UiY = new("ui.y");
        public static readonly EventKey DomainCreatedUpper = new("DOMAIN.CREATED");
        public static readonly EventKey DomainCreatedMixed = new("domain.Created");
        public static readonly EventKey UiRefreshMixed = new("UI.Refresh");
    }

    private static DefaultHttpContext CreateContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public void ResolveChains_SingleEvent_NoChain_ReturnsOnlyEvent()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions();
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.EventHappened);
        var resolved = bus.ResolveChains(context);

        // Assert
        Assert.Single(resolved);
        Assert.Contains("event.happened", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_SingleChain_ResolvesOneHop()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.DomainCreated, TestEvents.UiRefresh);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.DomainCreated);
        var resolved = bus.ResolveChains(context);

        // Assert
        Assert.Equal(2, resolved.Count);
        Assert.Contains("domain.created", resolved.Keys);
        Assert.Contains("ui.refresh", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_MultipleChains_FromSingleEvent_ResolvesAll()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.DomainCreated, TestEvents.UiRefresh, TestEvents.UiToast, TestEvents.UiUpdateCount);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.DomainCreated);
        var resolved = bus.ResolveChains(context);

        // Assert
        Assert.Equal(4, resolved.Count);
        Assert.Contains("domain.created", resolved.Keys);
        Assert.Contains("ui.refresh", resolved.Keys);
        Assert.Contains("ui.toast", resolved.Keys);
        Assert.Contains("ui.updateCount", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_TwoHopChain_DoesNotResolveTransitively()
    {
        // Arrange: A -> B -> C (two-hop chain)
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.A, TestEvents.B)
            .Chain(TestEvents.B, TestEvents.C);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act: Emit A
        bus.Emit(TestEvents.A);
        var resolved = bus.ResolveChains(context);

        // Assert: Should get A and B, but NOT C (no transitive resolution)
        Assert.Equal(2, resolved.Count);
        Assert.Contains("A", resolved.Keys);
        Assert.Contains("B", resolved.Keys);
        Assert.DoesNotContain("C", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_MultipleEmittedEvents_MergesAllChains()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.EventA, TestEvents.UiX)
            .Chain(TestEvents.EventB, TestEvents.UiY);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.EventA);
        bus.Emit(TestEvents.EventB);
        var resolved = bus.ResolveChains(context);

        // Assert
        Assert.Equal(4, resolved.Count);
        Assert.Contains("event.a", resolved.Keys);
        Assert.Contains("event.b", resolved.Keys);
        Assert.Contains("ui.x", resolved.Keys);
        Assert.Contains("ui.y", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_ChainedEventEmittedDirectly_IncludedOnce()
    {
        // Arrange: event.a chains to ui.refresh, but ui.refresh is also emitted directly
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.EventA, TestEvents.UiRefresh);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.EventA);
        bus.Emit(TestEvents.UiRefresh, new { message = "explicit" });
        var resolved = bus.ResolveChains(context);

        // Assert: ui.refresh appears only once, with the explicit payload
        Assert.Equal(2, resolved.Count);
        Assert.Contains("event.a", resolved.Keys);
        Assert.Contains("ui.refresh", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_ChainedEvent_InheritsPayload()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.DomainCreated, TestEvents.UiRefresh);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.DomainCreated, new { id = 123 });
        var resolved = bus.ResolveChains(context);

        // Assert
        Assert.Equal(2, resolved.Count);
        Assert.NotNull(resolved["domain.created"]); // Has payload
        Assert.NotNull(resolved["ui.refresh"]); // Chained event inherits payload
        Assert.Equal(resolved["domain.created"], resolved["ui.refresh"]);
    }

    [Fact]
    public void ResolveChains_NoPendingEvents_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.EventA, TestEvents.UiX);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act: Don't emit anything
        var resolved = bus.ResolveChains(context);

        // Assert
        Assert.Empty(resolved);
    }

    [Fact]
    public void ResolveChains_CaseInsensitive_EventNames()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.DomainCreatedMixed, TestEvents.UiRefreshMixed);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.DomainCreatedUpper);
        var resolved = bus.ResolveChains(context);

        // Assert: Case-insensitive matching
        Assert.Equal(2, resolved.Count);
        Assert.Contains("DOMAIN.CREATED", resolved.Keys);
        Assert.Contains("UI.Refresh", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_DuplicateChainedEvents_IncludedOnce()
    {
        // Arrange: Multiple events chain to the same UI event
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.EventA, TestEvents.UiRefresh)
            .Chain(TestEvents.EventB, TestEvents.UiRefresh);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit(TestEvents.EventA);
        bus.Emit(TestEvents.EventB);
        var resolved = bus.ResolveChains(context);

        // Assert: ui.refresh appears only once
        Assert.Equal(3, resolved.Count);
        Assert.Contains("event.a", resolved.Keys);
        Assert.Contains("event.b", resolved.Keys);
        Assert.Contains("ui.refresh", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_NoFiltering_AllEventsReturned()
    {
        // Arrange: This test verifies we DON'T filter based on client subscriptions
        // (The old system would have filtered events not in X-Swap-Events header)
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.DomainCreated, TestEvents.UiRefresh, TestEvents.UiToast);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act: Emit event (no X-Swap-Events header present)
        bus.Emit(TestEvents.DomainCreated);
        var resolved = bus.ResolveChains(context);

        // Assert: ALL events returned (no filtering)
        Assert.Equal(3, resolved.Count);
        Assert.Contains("domain.created", resolved.Keys);
        Assert.Contains("ui.refresh", resolved.Keys);
        Assert.Contains("ui.toast", resolved.Keys);
    }
}
