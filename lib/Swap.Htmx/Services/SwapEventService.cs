using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx.Services;

/// <summary>
/// Default implementation of ISwapEventService.
/// </summary>
public class SwapEventService : ISwapEventService
{
    private readonly IEventChainExecutor _executor;
    private readonly ILogger<SwapEventService> _logger;

    public SwapEventService(IEventChainExecutor executor, ILogger<SwapEventService> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public Swap.Htmx.Models.SwapResponseBuilder Response(ControllerBase controller)
    {
        var builder = new Swap.Htmx.Models.SwapResponseBuilder();
        
        // We need to cast to Controller to access View/PartialView methods
        // If it's just ControllerBase, we might be limited, but SwapResponseBuilder expects Controller
        if (controller is Controller mvcController)
        {
            builder.Controller = mvcController;
        }
        else
        {
            // Fallback or warning? 
            // SwapResponseBuilder relies on Controller.PartialView() for rendering.
            // If using Minimal APIs or ControllerBase, rendering might need a different approach (e.g. IRazorViewEngine directly).
            // For now, we assume Controller.
        }
        
        return builder;
    }

    public Swap.Htmx.Models.SwapResponseBuilder Event(EventKey eventKey, ControllerBase controller, object? payload = null)
    {
        var httpContext = controller.HttpContext;
        
        Dev.SwapDevLogger.LogSwapEvent(_logger, eventKey.Name, $"Payload: {payload?.GetType().Name ?? "null"}");
        
        var result = _executor.Execute(eventKey, httpContext, controller, payload);
        
        if (result != null)
        {
            if (payload != null)
            {
                result.WithTrigger(eventKey, payload);
            }
            return result;
        }

        // No chain configured - return empty builder
        var builder = Response(controller);
        
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }

    public async Task<Swap.Htmx.Models.SwapResponseBuilder> EventAsync(EventKey eventKey, ControllerBase controller, object? payload = null)
    {
        var httpContext = controller.HttpContext;
        
        Dev.SwapDevLogger.LogSwapEvent(_logger, eventKey.Name, $"Payload: {payload?.GetType().Name ?? "null"} (Async)");
        
        var result = await _executor.ExecuteAsync(eventKey, httpContext, controller, payload);
        
        if (result != null)
        {
            if (payload != null)
            {
                result.WithTrigger(eventKey, payload);
            }
            return result;
        }

        // No chain configured - return empty builder
        var builder = Response(controller);
        
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }
}
