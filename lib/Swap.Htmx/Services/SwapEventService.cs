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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SwapEventService(IEventChainExecutor executor, ILogger<SwapEventService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _executor = executor;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public SwapResponseBuilder Response()
    {
        return new SwapResponseBuilder();
    }

    public SwapResponseBuilder Response(ControllerBase controller)
    {
        var builder = new SwapResponseBuilder();
        
        // We need to cast to Controller to access View/PartialView methods
        // If it's just ControllerBase, we might be limited, but SwapResponseBuilder expects Controller
        if (controller is Controller mvcController)
        {
            builder.Controller = mvcController;
        }
        
        return builder;
    }

    public SwapResponseBuilder Event(EventKey eventKey, object? payload = null)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        
        Dev.SwapDevLogger.LogSwapEvent(_logger, eventKey.Name, $"Payload: {payload?.GetType().Name ?? "null"}");
        
        var result = _executor.Execute(eventKey, httpContext, null, payload);
        
        if (result != null)
        {
            if (payload != null)
            {
                result.WithTrigger(eventKey, payload);
            }
            return result;
        }

        // No chain configured - return empty builder
        var builder = Response();
        
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }

    public SwapResponseBuilder Event(EventKey eventKey, ControllerBase controller, object? payload = null)
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

    public async Task<SwapResponseBuilder> EventAsync(EventKey eventKey, object? payload = null)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        
        Dev.SwapDevLogger.LogSwapEvent(_logger, eventKey.Name, $"Payload: {payload?.GetType().Name ?? "null"} (Async)");
        
        var result = await _executor.ExecuteAsync(eventKey, httpContext, null, payload);
        
        if (result != null)
        {
            if (payload != null)
            {
                result.WithTrigger(eventKey, payload);
            }
            return result;
        }

        // No chain configured - return empty builder
        var builder = Response();
        
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }

    public async Task<SwapResponseBuilder> EventAsync(EventKey eventKey, ControllerBase controller, object? payload = null)
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
