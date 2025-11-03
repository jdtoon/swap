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
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act: run context middleware (populate active events), then emit, then build response header
        await ctxMw.InvokeAsync(context);
        await bus.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 1 });
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
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act
        await ctxMw.InvokeAsync(context);
        bus.Emit(SwapEvents.Entity.Created("product"), new { id = 2 });
        await respMw.InvokeAsync(context);

        // Assert
        var json = context.Response.Headers["HX-Trigger"].ToString();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
        Assert.Contains(SwapEvents.Entity.Created("product"), dict.Keys);
        Assert.Contains(SwapEvents.UI.RefreshList, dict.Keys);
    }

    [Fact]
    public async Task Response_Merges_With_Existing_HX_Trigger_Header()
    {
        // Arrange
        var context = CreateContext();
        context.Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            {"alpha", null}
        });

        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act
        await ctxMw.InvokeAsync(context);
    bus.Emit(SwapEvents.UI.RefreshList);
    await respMw.InvokeAsync(context);

        // Assert
        var json = context.Response.Headers["HX-Trigger"].ToString();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
        Assert.Contains("alpha", dict.Keys);
        Assert.Contains(SwapEvents.UI.RefreshList, dict.Keys);
    }

    [Fact]
    public async Task Multiple_Emits_Last_Payload_Wins()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions();

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

    await ctxMw.InvokeAsync(context);
    bus.Emit(SwapEvents.Entity.Created("product"), new { id = 1 });
    bus.Emit(SwapEvents.Entity.Created("product"), new { id = 2 });
    await respMw.InvokeAsync(context);

        var json = context.Response.Headers["HX-Trigger"].ToString();
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        var payload = dict[SwapEvents.Entity.Created("product")];
        Assert.True(payload.TryGetProperty("id", out var idProp));
        Assert.Equal(2, idProp.GetInt32());
    }

    [Fact]
    public async Task No_Pending_Events_Does_Not_Set_Header()
    {
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions();

        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        await ctxMw.InvokeAsync(context);
        await respMw.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("HX-Trigger"));
    }

    [Fact]
    public async Task Transitive_With_Filtering_Subset()
    {
        // Arrange
        var context = CreateContext();
        context.Request.Headers["X-Swap-Events"] = "b,c"; // only b and c
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions
        {
            ResolutionMode = ChainResolutionMode.Transitive,
            MaxTransitiveDepth = 2
        }
        .Chain("a", "b")
        .Chain("b", "c");

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        await ctxMw.InvokeAsync(context);
        bus.Emit("a");
        await respMw.InvokeAsync(context);

    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(context.Response.Headers["HX-Trigger"].ToString())!;
        Assert.DoesNotContain("a", dict.Keys); // filtered out
        Assert.Contains("b", dict.Keys);
        Assert.Contains("c", dict.Keys);
    }

    [Fact]
    public async Task Bidirectional_Filtering_Subset()
    {
        var context = CreateContext();
        context.Request.Headers["X-Swap-Events"] = "a"; // only parent allowed
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions { ResolutionMode = ChainResolutionMode.Bidirectional }
            .Chain("a", "b");

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        await ctxMw.InvokeAsync(context);
        bus.Emit("b"); // reverse should include 'a'
        await respMw.InvokeAsync(context);

    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(context.Response.Headers["HX-Trigger"].ToString())!;
        Assert.Contains("a", dict.Keys);
        Assert.DoesNotContain("b", dict.Keys); // filtered
    }

    [Fact]
    public async Task Multiple_Pending_Events_Merge()
    {
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain("a", "x")
            .Chain("b", "y");

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        await ctxMw.InvokeAsync(context);
        bus.Emit("a");
        bus.Emit("b");
        await respMw.InvokeAsync(context);

    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(context.Response.Headers["HX-Trigger"].ToString())!;
        Assert.Contains("a", dict.Keys);
        Assert.Contains("b", dict.Keys);
        Assert.Contains("x", dict.Keys);
        Assert.Contains("y", dict.Keys);
    }

    [Fact]
    public async Task Chained_Payload_Is_Null()
    {
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions().Chain("a", "b");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        await ctxMw.InvokeAsync(context);
        bus.Emit("a", new { id = 1 });
        await respMw.InvokeAsync(context);

    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(context.Response.Headers["HX-Trigger"].ToString())!;
        Assert.True(dict.TryGetValue("a", out var payload));
        Assert.True(payload.TryGetProperty("id", out _));
        Assert.True(dict.ContainsKey("b"));
        Assert.Equal(JsonValueKind.Null, dict["b"].ValueKind);
    }

    [Fact]
    public async Task Merge_Robustness_With_Invalid_Header_Values()
    {
        var context = CreateContext();
        // Two values: one valid JSON, one invalid
        context.Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new Dictionary<string, object?> { { "alpha", null } }));
        context.Response.Headers.Append("HX-Trigger", "not-json");

        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions().Chain("a", "b");
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var ctxMw = new SwapEventContextMiddleware(_ => Task.CompletedTask);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        await ctxMw.InvokeAsync(context);
        bus.Emit("a");
        await respMw.InvokeAsync(context);

    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(context.Response.Headers["HX-Trigger"].ToString())!;
        Assert.Contains("alpha", dict.Keys); // preserved
        Assert.Contains("a", dict.Keys);
        Assert.Contains("b", dict.Keys);
    }
}
