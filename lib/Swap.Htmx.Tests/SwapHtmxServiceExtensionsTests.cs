using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Guards the AddSwapHtmx registration overloads. The events-only overload must fully configure the
/// app (it previously skipped the SwapHtmxOptions singleton, so UseSwapHtmx()/SwapErrorMiddleware
/// failed at request time — a silent onboarding foot-gun).
/// </summary>
public class SwapHtmxServiceExtensionsTests
{
    [Fact]
    public void AddSwapHtmx_EventsOverload_FullyConfigures_IncludingSwapHtmxOptions()
    {
        var services = new ServiceCollection();

        // Explicitly-typed lambda selects the Action<SwapEventBusOptions> overload unambiguously.
        services.AddSwapHtmx((SwapEventBusOptions events) => events.When(new EventKey("test")));

        using var provider = services.BuildServiceProvider();

        // Was null before the fix -> UseSwapHtmx()'s SwapErrorMiddleware ctor would throw at runtime.
        Assert.NotNull(provider.GetService<SwapHtmxOptions>());
        Assert.NotNull(provider.GetService<SwapEventBusOptions>());
    }

    [Fact]
    public void AddSwapHtmx_FullOverload_RegistersSwapHtmxOptions()
    {
        var services = new ServiceCollection();
        services.AddSwapHtmx(options => options.EventBus.When(new EventKey("test")));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<SwapHtmxOptions>());
    }
}
