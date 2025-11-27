using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Models;

namespace Swap.Htmx.Events;

/// <summary>
/// Executes discovered event handlers for a given event.
/// </summary>
internal class SwapEventHandlerExecutor
{
    private readonly IServiceProvider _services;
    private readonly SwapEventHandlerRegistry _registry;

    public SwapEventHandlerExecutor(IServiceProvider services, SwapEventHandlerRegistry registry)
    {
        _services = services;
        _registry = registry;
    }

    /// <summary>
    /// Executes all handlers for the specified event type and payload.
    /// </summary>
    public async Task ExecuteHandlersAsync(Type eventType, object? payload, SwapResponseBuilder builder, CancellationToken ct = default)
    {
        var handlers = _registry.GetHandlers(eventType);
        if (handlers.Count == 0) return;

        using var scope = _services.CreateScope();
        foreach (var descriptor in handlers)
        {
            var handler = scope.ServiceProvider.GetRequiredService(descriptor.HandlerType);
            var method = handler.GetType().GetMethod("HandleAsync");
            if (method == null) continue;

            var task = (Task)method.Invoke(handler, new[] { payload, builder, ct })!;
            await task;
        }
    }
}