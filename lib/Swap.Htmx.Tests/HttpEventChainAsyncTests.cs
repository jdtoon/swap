using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

public class HttpEventChainAsyncTests
{
    private class TestController : SwapController
    {
        // Expose protected method for testing
        public Task<SwapResponseBuilder> SwapEventAsyncPublic(Events.EventKey eventKey, object? payload = null)
            => SwapEventAsync(eventKey, payload);
    }

    [Fact]
    public async Task When_ConfiguresAsyncEventChain()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        var eventKey = new EventKey("test.async");

        // Act
        options.When(eventKey)
            .RefreshPartialAsync("test-target", "_TestView", async ctx => 
            {
                await Task.Delay(10); // Simulate async work
                return new { Value = "Async Data" };
            });

        var configs = options.GetEventChainConfigs();

        // Assert
        Assert.True(configs.ContainsKey(eventKey.Name));
        var config = configs[eventKey.Name];
        Assert.Single(config.Partials);
        Assert.NotNull(config.Partials[0].ModelFactoryAsync);
    }

    [Fact]
    public async Task ExecuteAsync_RunsAsyncFactory()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        var eventKey = new EventKey("test.async.exec");
        
        options.When(eventKey)
            .RefreshPartialAsync("target", "view", async ctx => 
            {
                await Task.Delay(10);
                return "AsyncResult";
            });

        var executor = new EventChainExecutor(options);
        var httpContext = new DefaultHttpContext();
        var controller = new TestController();

        // Act
        var builder = await executor.ExecuteAsync(eventKey, httpContext, controller);

        // Assert
        Assert.NotNull(builder);
        // We can't easily inspect the builder's internal OobSwaps without reflection or exposing them,
        // but we can verify it didn't throw and returned a builder.
        // In a real integration test we would verify the OOB swap content.
    }
    
    [Fact]
    public async Task Execute_ThrowsOnAsyncFactory()
    {
        // Arrange
        var options = new SwapEventBusOptions();
        var eventKey = new EventKey("test.async.throw");
        
        options.When(eventKey)
            .RefreshPartialAsync("target", "view", async ctx => 
            {
                await Task.Delay(10);
                return "AsyncResult";
            });

        var executor = new EventChainExecutor(options);
        var httpContext = new DefaultHttpContext();
        var controller = new TestController();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            executor.Execute(eventKey, httpContext, controller));
    }
}
