using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Events;
using Swap.Htmx.Middleware;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapEventSystemTests
{
    private static DefaultHttpContext CreateContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task EventBus_WithChain_FiltersToActiveSubscriptions()
    {
        // Arrange
        var context = CreateContext();
        context.Request.Headers["X-Swap-Events"] = "ui.refreshList"; // only this is active

        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        // Prepare middlewares independently
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(_ => Task.CompletedTask, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act: run context middleware (populate active events), then emit, then build response header
        await ctxMw.InvokeAsync(context);
        await bus.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 1 });
        await context.Response.WriteAsync("OK");
    // Before invoking response middleware, ensure resolve/filter finds what we expect
    var probe = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
    var (resolvedProbe, beforeFilter) = probe.ResolveAndFilterFor(context);
    Assert.True(resolvedProbe.ContainsKey(SwapEvents.UI.RefreshList));

    await respMw.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.TryGetValue("HX-Trigger", out var header));
        var json = header.ToString();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
        Assert.DoesNotContain(SwapEvents.Entity.Created("product"), dict.Keys); // filtered out
        Assert.Contains(SwapEvents.UI.RefreshList, dict.Keys); // chained and active
    }

    [Fact]
    public async Task EventBus_NoActiveHeader_SendsOriginalAndChained()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);

        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(_ => Task.CompletedTask, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act
        await ctxMw.InvokeAsync(context);
        bus.Emit(SwapEvents.Entity.Created("product"), new { id = 2 });
        await context.Response.WriteAsync("OK");
    var probe = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
    var (resolvedProbe, beforeFilter) = probe.ResolveAndFilterFor(context);
    Assert.True(resolvedProbe.ContainsKey(SwapEvents.Entity.Created("product")));
    Assert.True(resolvedProbe.ContainsKey(SwapEvents.UI.RefreshList));

    await respMw.InvokeAsync(context);

        // Assert
        var json = context.Response.Headers["HX-Trigger"].ToString();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
        Assert.Contains(SwapEvents.Entity.Created("product"), dict.Keys);
        Assert.Contains(SwapEvents.UI.RefreshList, dict.Keys);
    }
}
