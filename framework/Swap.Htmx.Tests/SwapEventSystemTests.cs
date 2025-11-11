using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Events;
using Swap.Htmx.Middleware;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapEventSystemTests
{
    private static class TestEvents
    {
        public static readonly EventKey A = new("a");
        public static readonly EventKey B = new("b");
        public static readonly EventKey X = new("x");
        public static readonly EventKey Y = new("y");
    }
    private static DefaultHttpContext CreateContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task EventBus_WithChain_EmitsOriginalAndChained()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act: emit domain event, then build response header
        await bus.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 1 });
        await respMw.InvokeAsync(context);

        // Assert: both original event and chained UI event are included
        Assert.True(context.Response.Headers.TryGetValue("HX-Trigger", out var header));
        var json = header.ToString();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
        Assert.Contains(SwapEvents.Entity.Created("product"), dict.Keys);
        Assert.Contains(SwapEvents.UI.RefreshList, dict.Keys);
    }

    [Fact]
    public async Task EventBus_NoChain_SendsOriginalOnly()
    {
        // Arrange
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions();

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act
        bus.Emit(SwapEvents.Entity.Created("product"), new { id = 2 });
        await respMw.InvokeAsync(context);

        // Assert
        var json = context.Response.Headers["HX-Trigger"].ToString();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
        Assert.Contains(SwapEvents.Entity.Created("product"), dict.Keys);
        Assert.Single(dict.Keys);
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
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        // Act
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
        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

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

        var respMw = new SwapEventResponseMiddleware(async ctx =>
        {
            await ctx.Response.WriteAsync("OK");
        }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        await respMw.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("HX-Trigger"));
    }

    [Fact]
    public async Task Multiple_Pending_Events_Merge()
    {
        var context = CreateContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = new SwapEventBusOptions()
            .Chain(TestEvents.A, TestEvents.X)
            .Chain(TestEvents.B, TestEvents.Y);

        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        bus.Emit(TestEvents.A);
        bus.Emit(TestEvents.B);
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
        var options = new SwapEventBusOptions().Chain(TestEvents.A, TestEvents.B);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        bus.Emit(TestEvents.A, new { id = 1 });
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
        var options = new SwapEventBusOptions().Chain(TestEvents.A, TestEvents.B);
        var bus = new SwapEventBus(accessor, options, NullLogger<SwapEventBus>.Instance);
        var respMw = new SwapEventResponseMiddleware(async ctx => { await ctx.Response.WriteAsync("OK"); }, accessor, options, NullLogger<SwapEventResponseMiddleware>.Instance);

        bus.Emit(TestEvents.A);
        await respMw.InvokeAsync(context);

    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(context.Response.Headers["HX-Trigger"].ToString())!;
        Assert.Contains("alpha", dict.Keys); // preserved
        Assert.Contains("a", dict.Keys);
        Assert.Contains("b", dict.Keys);
    }
}
