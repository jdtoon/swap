using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Events;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.Realtime;

public class RealtimeEventMiddlewareTests
{
    private sealed class RecordingBridge : IRealtimeEventBridge
    {
        public int Calls { get; private set; }
        public string? LastEventName { get; private set; }
        public object? LastPayload { get; private set; }
        public bool ThrowOnCall { get; init; }

        public Task HandleRealtimeEventAsync(string eventName, object? payload, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastEventName = eventName;
            LastPayload = payload;

            if (ThrowOnCall)
            {
                throw new InvalidOperationException("boom");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingRegistry : IRealtimeConnectionRegistry
    {
        public int BroadcastCalls { get; private set; }

        public void RegisterConnection(IRealtimeConnection connection) { }

        public void UnregisterConnection(string connectionId) { }

        public Task BroadcastAsync(string eventName, string html, CancellationToken ct = default)
        {
            BroadcastCalls++;
            throw new InvalidOperationException("broadcast failed");
        }

        public Task BroadcastToRoomsAsync(string eventName, string html, IEnumerable<string> rooms, CancellationToken ct = default) => Task.CompletedTask;
        public Task BroadcastToSubscribersAsync(string eventName, string html, CancellationToken ct = default) => Task.CompletedTask;
        public Task BroadcastToFilteredAsync(string eventName, string data, Func<IRealtimeConnection, bool> filter, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task BroadcastToRolesAsync(string eventName, string html, IEnumerable<string> roles, CancellationToken ct = default) => Task.CompletedTask;
        public Task BroadcastToUserAsync(string eventName, string html, string userId, CancellationToken ct = default) => Task.CompletedTask;

        public IReadOnlyCollection<string> GetActiveConnectionIds() => Array.Empty<string>();

        public RealtimeConnectionStats GetStats() => new(
            TotalConnections: 0,
            ActiveConnections: 0,
            ConnectionsByRoom: new Dictionary<string, int>(),
            SubscriptionsByEvent: new Dictionary<string, int>());
    }

    private sealed class StartedApp : IAsyncDisposable
    {
        private readonly WebApplication _app;
        public HttpClient Client { get; }

        public StartedApp(WebApplication app)
        {
            _app = app;
            Client = app.GetTestClient();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
        }
    }

    private static async Task<StartedApp> StartAppAsync(
        Action<IServiceCollection> configureServices,
        Action<WebApplication> configureApp)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(new SwapEventBusOptions());
        builder.Services.AddScoped<ISwapEventBus>(sp => new SwapEventBus(
            sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            sp.GetRequiredService<SwapEventBusOptions>(),
            NullLogger<SwapEventBus>.Instance));

        configureServices(builder.Services);

        var app = builder.Build();
        app.UseSseEventBridge();
        configureApp(app);

        await app.StartAsync();
        return new StartedApp(app);
    }

    [Fact]
    public async Task InvokeAsync_BridgeMissing_DoesNotThrowAndResponseCompletes()
    {
        await using var app = await StartAppAsync(
            configureServices: _ => { },
            configureApp: app =>
            {
                app.MapGet("/test", (ISwapEventBus bus) =>
                {
                    bus.Emit(new EventKey("sse:broadcast:test-event"));
                    return Microsoft.AspNetCore.Http.TypedResults.Text("ok");
                });
            });

        var body = await app.Client.GetStringAsync("/test");
        Assert.Equal("ok", body);
    }

    [Fact]
    public async Task InvokeAsync_BridgeThrows_DoesNotThrowAndResponseCompletes()
    {
        var bridge = new RecordingBridge { ThrowOnCall = true };

        await using var app = await StartAppAsync(
            configureServices: services => services.AddSingleton<IRealtimeEventBridge>(bridge),
            configureApp: app =>
            {
                app.MapGet("/test", (ISwapEventBus bus) =>
                {
                    bus.Emit(new EventKey("sse:broadcast:test-event"), new { id = 123 });
                    return Microsoft.AspNetCore.Http.TypedResults.Text("ok");
                });
            });

        var body = await app.Client.GetStringAsync("/test");
        Assert.Equal("ok", body);
        Assert.Equal(1, bridge.Calls);
        Assert.Equal("sse:broadcast:test-event", bridge.LastEventName);
    }

    [Fact]
    public async Task InvokeAsync_BroadcastFailure_DoesNotBreakHttpResponse()
    {
        var registry = new ThrowingRegistry();

        await using var app = await StartAppAsync(
            configureServices: services =>
            {
                services.AddSingleton<IRealtimeConnectionRegistry>(registry);
                services.AddSingleton<ISseViewRenderer>(new StubSseViewRenderer());
                services.AddScoped<IRealtimeEventBridge>(sp => new RealtimeEventBridge(
                    sp.GetRequiredService<IRealtimeConnectionRegistry>(),
                    sp,
                    sp.GetRequiredService<SwapEventBusOptions>(),
                    NullLogger<RealtimeEventBridge>.Instance,
                    sp.GetRequiredService<ISseViewRenderer>()));
            },
            configureApp: app =>
            {
                app.MapGet("/test", (ISwapEventBus bus) =>
                {
                    bus.Emit(new EventKey("sse:broadcast:test-event"));
                    return Microsoft.AspNetCore.Http.TypedResults.Text("ok");
                });
            });

        var body = await app.Client.GetStringAsync("/test");
        Assert.Equal("ok", body);
        Assert.Equal(1, registry.BroadcastCalls);
    }

    private sealed class StubSseViewRenderer : ISseViewRenderer
    {
        public Task<string> RenderPartialAsync<TModel>(string viewName, TModel model) => Task.FromResult("<div />");
    }
}
