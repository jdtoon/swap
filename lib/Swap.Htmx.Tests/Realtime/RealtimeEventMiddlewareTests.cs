using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Swap.Htmx.Events;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.Realtime;

public class RealtimeEventMiddlewareTests
{
    private static DefaultHttpContext CreateContext(ServiceProvider services)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ServiceProvider BuildServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton(new SwapEventBusOptions());
        services.AddSingleton<ISwapEventBus>(sp =>
            new SwapEventBus(
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<SwapEventBusOptions>(),
                NullLogger<SwapEventBus>.Instance));

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static async Task<string> ReadBodyAsync(HttpResponse response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_BridgeMissing_DoesNotThrowAndResponseCompletes()
    {
        // Arrange
        var services = BuildServices();
        var context = CreateContext(services);
        (services.GetRequiredService<IHttpContextAccessor>() as HttpContextAccessor)!.HttpContext = context;

        var middleware = new RealtimeEventMiddleware(async ctx =>
        {
            // Emit a realtime-prefixed event (would normally be produced by SwapResponseBuilder triggers)
            var bus = ctx.RequestServices.GetRequiredService<ISwapEventBus>();
            bus.Emit(new EventKey("sse:broadcast:test-event"));

            await ctx.Response.WriteAsync("ok");
        }, NullLogger<RealtimeEventMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("ok", await ReadBodyAsync(context.Response));
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BridgeThrows_DoesNotThrowAndResponseCompletes()
    {
        // Arrange
        var bridge = new Mock<IRealtimeEventBridge>();
        bridge
            .Setup(b => b.HandleRealtimeEventAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var services = BuildServices(sc =>
        {
            sc.AddSingleton<IRealtimeEventBridge>(bridge.Object);
        });

        var context = CreateContext(services);
        (services.GetRequiredService<IHttpContextAccessor>() as HttpContextAccessor)!.HttpContext = context;

        var middleware = new RealtimeEventMiddleware(async ctx =>
        {
            var bus = ctx.RequestServices.GetRequiredService<ISwapEventBus>();
            bus.Emit(new EventKey("sse:broadcast:test-event"), new { id = 123 });
            await ctx.Response.WriteAsync("ok");
        }, NullLogger<RealtimeEventMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("ok", await ReadBodyAsync(context.Response));
        bridge.Verify(b => b.HandleRealtimeEventAsync(
            "sse:broadcast:test-event",
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_BroadcastFailure_DoesNotBreakHttpResponse()
    {
        // Arrange: a real bridge that fails during broadcast
        var registry = new Mock<IRealtimeConnectionRegistry>();
        registry
            .Setup(r => r.BroadcastAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broadcast failed"));

        var viewRenderer = new Mock<ISseViewRenderer>();
        viewRenderer
            .Setup(v => v.RenderPartialAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div />");

        var options = new SwapEventBusOptions();

        var services = BuildServices(sc =>
        {
            sc.AddSingleton(options);
            sc.AddSingleton<IRealtimeConnectionRegistry>(registry.Object);
            sc.AddSingleton<ISseViewRenderer>(viewRenderer.Object);
            sc.AddSingleton<IRealtimeEventBridge>(sp => new RealtimeEventBridge(
                sp.GetRequiredService<IRealtimeConnectionRegistry>(),
                sp,
                sp.GetRequiredService<SwapEventBusOptions>(),
                NullLogger<RealtimeEventBridge>.Instance,
                sp.GetRequiredService<ISseViewRenderer>()));
        });

        var context = CreateContext(services);
        (services.GetRequiredService<IHttpContextAccessor>() as HttpContextAccessor)!.HttpContext = context;

        var middleware = new RealtimeEventMiddleware(async ctx =>
        {
            var bus = ctx.RequestServices.GetRequiredService<ISwapEventBus>();
            bus.Emit(new EventKey("sse:broadcast:test-event"));
            await ctx.Response.WriteAsync("ok");
        }, NullLogger<RealtimeEventMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("ok", await ReadBodyAsync(context.Response));
        registry.Verify(r => r.BroadcastAsync(
            "test-event",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
