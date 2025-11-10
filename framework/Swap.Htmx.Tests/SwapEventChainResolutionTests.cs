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
        bus.Emit("event.happened");
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
            .Chain("domain.created", "ui.refresh");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit("domain.created");
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
            .Chain("domain.created", "ui.refresh", "ui.toast", "ui.updateCount");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit("domain.created");
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
            .Chain("A", "B")
            .Chain("B", "C");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act: Emit A
        bus.Emit("A");
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
            .Chain("event.a", "ui.x")
            .Chain("event.b", "ui.y");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit("event.a");
        bus.Emit("event.b");
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
            .Chain("event.a", "ui.refresh");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit("event.a");
        bus.Emit("ui.refresh", new { message = "explicit" });
        var resolved = bus.ResolveChains(context);

        // Assert: ui.refresh appears only once, with the explicit payload
        Assert.Equal(2, resolved.Count);
        Assert.Contains("event.a", resolved.Keys);
        Assert.Contains("ui.refresh", resolved.Keys);
    }

    [Fact]
    public void ResolveChains_ChainedEvent_HasNullPayload()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain("domain.created", "ui.refresh");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit("domain.created", new { id = 123 });
        var resolved = bus.ResolveChains(context);

        // Assert
        Assert.Equal(2, resolved.Count);
        Assert.NotNull(resolved["domain.created"]); // Has payload
        Assert.Null(resolved["ui.refresh"]); // Chained event has null payload
    }

    [Fact]
    public void ResolveChains_NoPendingEvents_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain("event.a", "ui.x");
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
            .Chain("domain.Created", "UI.Refresh");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit("DOMAIN.CREATED");
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
            .Chain("event.a", "ui.refresh")
            .Chain("event.b", "ui.refresh");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act
        bus.Emit("event.a");
        bus.Emit("event.b");
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
            .Chain("domain.created", "ui.refresh", "ui.toast");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Act: Emit event (no X-Swap-Events header present)
        bus.Emit("domain.created");
        var resolved = bus.ResolveChains(context);

        // Assert: ALL events returned (no filtering)
        Assert.Equal(3, resolved.Count);
        Assert.Contains("domain.created", resolved.Keys);
        Assert.Contains("ui.refresh", resolved.Keys);
        Assert.Contains("ui.toast", resolved.Keys);
    }
}
