using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Guards that <see cref="SwapRealtimeServiceExtensions.AddSseEventBridge"/> registers
/// <see cref="IRealtimePresence"/> in DI so consumers get it without hand-registering.
/// </summary>
public class PresenceDiTests
{
    [Fact]
    public void AddSseEventBridge_RegistersIRealtimePresence_AsInMemorySingleton()
    {
        var services = new ServiceCollection();
        services.AddSseEventBridge();

        using var provider = services.BuildServiceProvider();

        var presence = provider.GetService<IRealtimePresence>();

        Assert.NotNull(presence);
        Assert.IsType<InMemoryRealtimePresence>(presence);
    }

    [Fact]
    public void AddSseEventBridge_IRealtimePresence_ResolvesSameInstance_AcrossResolutions()
    {
        var services = new ServiceCollection();
        services.AddSseEventBridge();

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IRealtimePresence>();
        var second = provider.GetRequiredService<IRealtimePresence>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddSseEventBridge_UserRegisteredPresence_TakesPrecedence()
    {
        var services = new ServiceCollection();
        var custom = new InMemoryRealtimePresence();
        services.AddSingleton<IRealtimePresence>(custom);

        services.AddSseEventBridge();

        using var provider = services.BuildServiceProvider();

        Assert.Same(custom, provider.GetRequiredService<IRealtimePresence>());
    }
}
