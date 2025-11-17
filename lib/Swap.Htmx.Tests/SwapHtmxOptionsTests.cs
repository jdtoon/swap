using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapHtmxOptionsTests
{
    [Fact]
    public void SwapHtmxOptions_DefaultSearchPaths_ContainsShared()
    {
        // Arrange & Act
        var options = new SwapHtmxOptions();

        // Assert
        Assert.Single(options.PartialViewSearchPaths);
        Assert.Contains("Shared", options.PartialViewSearchPaths);
    }

    [Fact]
    public void SwapHtmxOptions_EventBus_IsInitialized()
    {
        // Arrange & Act
        var options = new SwapHtmxOptions();

        // Assert
        Assert.NotNull(options.EventBus);
    }

    [Fact]
    public void SwapHtmxOptions_CanAddCustomSearchPaths()
    {
        // Arrange
        var options = new SwapHtmxOptions();

        // Act
        options.PartialViewSearchPaths.Add("Components");
        options.PartialViewSearchPaths.Add("Cart");

        // Assert
        Assert.Equal(3, options.PartialViewSearchPaths.Count);
        Assert.Contains("Shared", options.PartialViewSearchPaths);
        Assert.Contains("Components", options.PartialViewSearchPaths);
        Assert.Contains("Cart", options.PartialViewSearchPaths);
    }

    [Fact]
    public void AddSwapHtmx_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSwapHtmx();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<SwapHtmxOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void AddSwapHtmx_WithConfiguration_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSwapHtmx(options =>
        {
            options.PartialViewSearchPaths.Add("Cart");
            options.PartialViewSearchPaths.Add("Products");
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<SwapHtmxOptions>();
        Assert.Contains("Shared", options.PartialViewSearchPaths);
        Assert.Contains("Cart", options.PartialViewSearchPaths);
        Assert.Contains("Products", options.PartialViewSearchPaths);
    }

    [Fact]
    public void AddSwapHtmx_WithConfiguration_ConfiguresEventBus()
    {
        // Arrange
        var services = new ServiceCollection();
        var testEvent = new EventKey("test.event");

        // Act
        services.AddSwapHtmx(options =>
        {
            options.EventBus.When(testEvent)
                .Toast("Test", ToastType.Success);
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var eventBusOptions = provider.GetRequiredService<SwapEventBusOptions>();
        Assert.NotNull(eventBusOptions);
        
        var configs = eventBusOptions.GetEventChainConfigs();
        Assert.Contains(testEvent.Name, configs.Keys);
    }

    [Fact]
    public void AddSwapHtmx_RegistersEventBusOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSwapHtmx();
        var provider = services.BuildServiceProvider();

        // Assert
        var eventBusOptions = provider.GetService<SwapEventBusOptions>();
        Assert.NotNull(eventBusOptions);
    }

    [Fact]
    public void AddSwapHtmx_RegistersEventChainExecutor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();

        // Act
        services.AddSwapHtmx();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var executor = scope.ServiceProvider.GetService<IEventChainExecutor>();
        Assert.NotNull(executor);
    }

    [Fact]
    public void AddSwapHtmx_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSwapHtmx();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<SwapHtmxOptions>();
        Assert.Single(options.PartialViewSearchPaths);
        Assert.Equal("Shared", options.PartialViewSearchPaths[0]);
    }
}
