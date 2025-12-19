using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using Swap.Htmx.Services;
using Xunit;

namespace Swap.Htmx.Tests;

public class HttpEventChainTests
{
    private class TestController : SwapController
    {
        // Expose protected method for testing
        public SwapResponseBuilder SwapEventPublic(Events.EventKey eventKey, object? payload = null)
            => SwapEvent(eventKey, payload);
    }

    [Fact]
    public void When_ConfiguresEventChain()
    {
        // Arrange
        var options = new SwapEventBusOptions();

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("test-target", "_TestView")
            .SuccessToast("Test message");

        var configs = options.GetEventChainConfigs();

        // Assert
        Assert.True(configs.ContainsKey(SwapEvents.UI.RefreshList.Name));
        var config = configs[SwapEvents.UI.RefreshList.Name];
        Assert.Single(config.Partials);
        Assert.Single(config.Toasts);
        Assert.Equal("test-target", config.Partials[0].TargetId);
        Assert.Equal("_TestView", config.Partials[0].ViewName);
        Assert.Equal("Test message", config.Toasts[0].Message);
        Assert.Equal(ToastType.Success, config.Toasts[0].Type);
    }

    [Fact]
    public void When_ChainMultiplePartials()
    {
        // Arrange
        var options = new SwapEventBusOptions();

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("target1", "_View1")
            .RefreshPartial("target2", "_View2", swapMode: SwapMode.InnerHTML)
            .RefreshPartial("target3", "_View3", swapMode: SwapMode.BeforeEnd);

        var config = options.GetEventChainConfigs()[SwapEvents.UI.RefreshList.Name];

        // Assert
        Assert.Equal(3, config.Partials.Count);
        Assert.Equal("target1", config.Partials[0].TargetId);
        Assert.Equal(SwapMode.OuterHTML, config.Partials[0].SwapMode);
        Assert.Equal("target2", config.Partials[1].TargetId);
        Assert.Equal(SwapMode.InnerHTML, config.Partials[1].SwapMode);
        Assert.Equal("target3", config.Partials[2].TargetId);
        Assert.Equal(SwapMode.BeforeEnd, config.Partials[2].SwapMode);
    }

    [Fact]
    public void When_ChainMultipleToasts()
    {
        // Arrange
        var options = new SwapEventBusOptions();

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .SuccessToast("Success!")
            .ErrorToast("Error!")
            .WarningToast("Warning!")
            .InfoToast("Info!");

        var config = options.GetEventChainConfigs()[SwapEvents.UI.RefreshList.Name];

        // Assert
        Assert.Equal(4, config.Toasts.Count);
        Assert.Equal(ToastType.Success, config.Toasts[0].Type);
        Assert.Equal(ToastType.Error, config.Toasts[1].Type);
        Assert.Equal(ToastType.Warning, config.Toasts[2].Type);
        Assert.Equal(ToastType.Info, config.Toasts[3].Type);
    }

    [Fact]
    public void When_ChainTriggerEvents()
    {
        // Arrange
        var options = new SwapEventBusOptions();

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .AlsoTrigger(SwapEvents.UI.UpdateCounter)
            .AlsoTrigger(SwapEvents.Notification.Info);

        var config = options.GetEventChainConfigs()[SwapEvents.UI.RefreshList.Name];

        // Assert
        Assert.Equal(2, config.TriggerEvents.Count);
        Assert.Equal(SwapEvents.UI.UpdateCounter.Name, config.TriggerEvents[0].Name);
        Assert.Equal(SwapEvents.Notification.Info.Name, config.TriggerEvents[1].Name);
    }

    [Fact]
    public void When_BuildReturnsOptions()
    {
        // Arrange
        var options = new SwapEventBusOptions();

        // Act
        var result = options.When(SwapEvents.UI.RefreshList)
            .SuccessToast("Test")
            .Build();

        // Assert
        Assert.Same(options, result);
    }

    [Fact]
    public void RefreshPartial_WithModelFactory()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        var wasCalled = false;

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("target", "_View", ctx =>
            {
                wasCalled = true;
                return new { Data = "test" };
            });

        var config = options.GetEventChainConfigs()[SwapEvents.UI.RefreshList.Name];
        var httpContext = new DefaultHttpContext();
        var model = config.Partials[0].ModelFactory?.Invoke(httpContext);

        // Assert
        Assert.True(wasCalled);
        Assert.NotNull(model);
    }

    [Fact]
    public void EventChainExecutor_NoChainConfigured_ReturnsNull()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        var executor = new EventChainExecutor(options);
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EventChainExecutor_WithPartials_BuildsResponse()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("target1", "_View1")
            .RefreshPartial("target2", "_View2");

        var executor = new EventChainExecutor(options);
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.OobSwaps.Count());
        Assert.Equal("target1", result.OobSwaps.First().TargetId);
        Assert.Equal("_View1", result.OobSwaps.First().ViewName);
    }

    [Fact]
    public void EventChainExecutor_WithToasts_BuildsResponse()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        options.When(SwapEvents.UI.RefreshList)
            .SuccessToast("Success!")
            .ErrorToast("Error!");

        var executor = new EventChainExecutor(options);
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.Toasts.Count());
    }

    [Fact]
    public void EventChainExecutor_WithTriggers_BuildsResponse()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        options.When(SwapEvents.UI.RefreshList)
            .AlsoTrigger(SwapEvents.UI.UpdateCounter)
            .AlsoTrigger(SwapEvents.Notification.Info);

        var executor = new EventChainExecutor(options);
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.Triggers.Count());
        Assert.Contains(result.Triggers, t => t.EventName == SwapEvents.UI.UpdateCounter.Name);
        Assert.Contains(result.Triggers, t => t.EventName == SwapEvents.Notification.Info.Name);
    }

    [Fact]
    public void EventChainExecutor_ModelFactory_InvokedWithHttpContext()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        HttpContext? capturedContext = null;

        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("target", "_View", ctx =>
            {
                capturedContext = ctx;
                return new { Test = "data" };
            });

        var executor = new EventChainExecutor(options);
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller);

        // Assert - model factory IS invoked during Execute with the HttpContext
        Assert.NotNull(result);
        Assert.Same(httpContext, capturedContext);
        var partial = result!.OobSwaps.First();
        Assert.NotNull(partial.Model); // Model should be set from factory
    }

    [Fact]
    public void SwapEvent_WithNoExecutor_ReturnsEmptyBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<ISwapEventBus, SwapEventBus>();
        services.AddSingleton<SwapEventBusOptions>(new SwapEventBusOptions());
        services.AddScoped<IEventChainExecutor>(sp => new EventChainExecutor(new SwapEventBusOptions()));
        services.AddScoped<SwapEventHandlerRegistry>(sp => new SwapEventHandlerRegistry());
        services.AddScoped<SwapEventHandlerExecutor>();
        services.AddScoped<ISwapEventService, SwapEventService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        // Act
        var result = controller.SwapEventPublic(SwapEvents.UI.RefreshList);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.OobSwaps);
        Assert.Empty(result.Toasts);
        // Note: triggers may include the event itself depending on payload
    }

    [Fact]
    public void SwapEvent_WithExecutor_ExecutesChain()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        
        var options = new SwapEventBusOptions();
        options.When(SwapEvents.UI.RefreshList)
            .SuccessToast("Refreshed!");

        services.AddSingleton(options);
        services.AddSingleton<ISwapEventBus, SwapEventBus>();
        services.AddScoped<IEventChainExecutor>(sp => new EventChainExecutor(options));
        services.AddScoped<SwapEventHandlerRegistry>(sp => new SwapEventHandlerRegistry());
        services.AddScoped<SwapEventHandlerExecutor>();
        services.AddScoped<ISwapEventService, SwapEventService>();

        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        // Act
        var result = controller.SwapEventPublic(SwapEvents.UI.RefreshList);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Toasts);
        Assert.Equal("Refreshed!", result.Toasts.First().Message);
    }

    [Fact]
    public void SwapEvent_WithPayload_AddsTriggerWithPayload()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<ISwapEventBus, SwapEventBus>();
        services.AddSingleton<SwapEventBusOptions>(new SwapEventBusOptions());
        services.AddScoped<IEventChainExecutor>(sp => new EventChainExecutor(new SwapEventBusOptions()));
        services.AddScoped<SwapEventHandlerRegistry>(sp => new SwapEventHandlerRegistry());
        services.AddScoped<SwapEventHandlerExecutor>();
        services.AddScoped<ISwapEventService, SwapEventService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
        var payload = new { Id = 123, Name = "Test" };

        // Act
        var result = controller.SwapEventPublic(SwapEvents.UI.RefreshList, payload);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Triggers);
        Assert.Equal(SwapEvents.UI.RefreshList.Name, result.Triggers.First().EventName);
        Assert.Same(payload, result.Triggers.First().Payload);
    }

    [Fact]
    public void CompleteEventChain_AllFeatures()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        options.When(SwapEvents.Entity.Created("Product"))
            .RefreshPartial("list", "_List", ctx => new { Items = new[] { 1, 2, 3 } })
            .RefreshPartial("count", "_Count", ctx => new { Count = 3 }, SwapMode.InnerHTML)
            .SuccessToast("Created successfully!")
            .AlsoTrigger(SwapEvents.UI.UpdateCounter)
            .AlsoTrigger(SwapEvents.Notification.Info);

        var executor = new EventChainExecutor(options);
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();

        // Act
        var result = executor.Execute(SwapEvents.Entity.Created("Product"), httpContext, controller);

        // Assert
        Assert.NotNull(result);
        
        // Partials
        Assert.Equal(2, result!.OobSwaps.Count());
        Assert.Equal("list", result.OobSwaps.First().TargetId);
        Assert.Equal("count", result.OobSwaps.Last().TargetId);
        
        // Toasts
        Assert.Single(result.Toasts);
        Assert.Equal("Created successfully!", result.Toasts.First().Message);
        Assert.Equal(ToastType.Success, result.Toasts.First().Type);
        
        // Triggers
        Assert.Equal(2, result.Triggers.Count());
        Assert.Contains(result.Triggers, t => t.EventName == SwapEvents.UI.UpdateCounter.Name);
        Assert.Contains(result.Triggers, t => t.EventName == SwapEvents.Notification.Info.Name);
    }

    [Fact]
    public void MultipleEventChains_Independent()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        
        options.When(SwapEvents.Entity.Created("Product"))
            .SuccessToast("Created!");
        
        options.When(SwapEvents.Entity.Updated("Product"))
            .InfoToast("Updated!");
        
        options.When(SwapEvents.Entity.Deleted("Product"))
            .WarningToast("Deleted!");

        var executor = new EventChainExecutor(options);
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();

        // Act
        var result1 = executor.Execute(SwapEvents.Entity.Created("Product"), httpContext, controller);
        var result2 = executor.Execute(SwapEvents.Entity.Updated("Product"), httpContext, controller);
        var result3 = executor.Execute(SwapEvents.Entity.Deleted("Product"), httpContext, controller);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal("Created!", result1!.Toasts.First().Message);
        Assert.Equal(ToastType.Success, result1.Toasts.First().Type);

        Assert.NotNull(result2);
        Assert.Equal("Updated!", result2!.Toasts.First().Message);
        Assert.Equal(ToastType.Info, result2.Toasts.First().Type);

        Assert.NotNull(result3);
        Assert.Equal("Deleted!", result3!.Toasts.First().Message);
        Assert.Equal(ToastType.Warning, result3.Toasts.First().Type);
    }

    [Fact]
    public void EventChain_WithRedirect_ConfiguresRedirect()
    {
        // Arrange
        var options = new SwapEventBusOptions();

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .Toast("Success!", ToastType.Success)
            .Redirect("/dashboard");

        var configs = options.GetEventChainConfigs();

        // Assert
        var config = configs[SwapEvents.UI.RefreshList.Name];
        Assert.NotNull(config.Redirect);
        Assert.Equal("/dashboard", config.Redirect!.Url);
    }

    [Fact]
    public void EventChainExecutor_WithRedirect_BuildsResponseWithRedirect()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        options.When(SwapEvents.UI.RefreshList)
            .Redirect("/orders");

        var executor = new Events.EventChainExecutor(options);
        var httpContext = new DefaultHttpContext();
        var controller = new TestController();

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/orders", result!.RedirectUrl);
    }

    [Fact]
    public void SwapResponseBuilder_WithRedirect_StoresRedirectUrl()
    {
        // Arrange
        var builder = new SwapResponseBuilder();


        // Act
        builder.WithRedirect("/checkout");

        // Assert
        Assert.Equal("/checkout", builder.RedirectUrl);
    }

    [Fact]
    public void RefreshPartial_WithPayloadAwareFactory_StoresFactoryWithPayload()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        Func<HttpContext, object?, object?> factory = (ctx, payload) => payload;

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("test-target", "_TestView", factory);

        var configs = options.GetEventChainConfigs();

        // Assert
        var config = configs[SwapEvents.UI.RefreshList.Name];
        Assert.Single(config.Partials);
        Assert.Null(config.Partials[0].ModelFactory); // Standard factory should be null
        Assert.NotNull(config.Partials[0].ModelFactoryWithPayload); // Payload factory should be set
    }

    [Fact]
    public void RefreshPartial_WithStandardFactory_StoresFactoryWithoutPayload()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        Func<HttpContext, object?> factory = ctx => new { Test = "data" };

        // Act
        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("test-target", "_TestView", factory);

        var configs = options.GetEventChainConfigs();

        // Assert
        var config = configs[SwapEvents.UI.RefreshList.Name];
        Assert.Single(config.Partials);
        Assert.NotNull(config.Partials[0].ModelFactory); // Standard factory should be set
        Assert.Null(config.Partials[0].ModelFactoryWithPayload); // Payload factory should be null
    }

    [Fact]
    public void EventChainExecutor_WithPayloadFactory_PassesPayloadToFactory()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        var testPayload = new { OrderId = 42, Status = "Shipped" };
        object? capturedPayload = null;

        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("test-target", "_TestView", (ctx, payload) =>
            {
                capturedPayload = payload;
                return payload;
            });

        var executor = new EventChainExecutor(options);
        var httpContext = new DefaultHttpContext();
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TestTempDataDictionary()
        };

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller, testPayload);

        // Assert
        Assert.NotNull(result);
        Assert.Same(testPayload, capturedPayload);
    }

    [Fact]
    public void EventChainExecutor_WithStandardFactory_DoesNotReceivePayload()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        var testPayload = new { OrderId = 42 };
        var factoryCalled = false;

        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("test-target", "_TestView", ctx =>
            {
                factoryCalled = true;
                return new { Data = "from-factory" };
            });

        var executor = new EventChainExecutor(options);
        var httpContext = new DefaultHttpContext();
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TestTempDataDictionary()
        };

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller, testPayload);

        // Assert
        Assert.NotNull(result);
        Assert.True(factoryCalled);
    }

    [Fact]
    public void EventChainExecutor_WithoutPayload_PayloadFactoryReceivesNull()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        object? capturedPayload = new { Sentinel = "not-null" }; // Start with non-null

        options.When(SwapEvents.UI.RefreshList)
            .RefreshPartial("test-target", "_TestView", (ctx, payload) =>
            {
                capturedPayload = payload;
                return new { };
            });

        var executor = new EventChainExecutor(options);
        var httpContext = new DefaultHttpContext();
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TestTempDataDictionary()
        };

        // Act
        var result = executor.Execute(SwapEvents.UI.RefreshList, httpContext, controller, payload: null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(capturedPayload); // Should receive null if no payload provided
    }
}


