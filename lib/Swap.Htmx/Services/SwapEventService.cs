using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Diagnostics;
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
    private readonly ISwapEventBus _eventBus;

    public SwapEventService(
        IEventChainExecutor executor, 
        ILogger<SwapEventService> logger, 
        IHttpContextAccessor httpContextAccessor,
        ISwapEventBus eventBus)
    {
        _executor = executor;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _eventBus = eventBus;
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

    public SwapResponseBuilder Response(PageModel pageModel)
    {
        return new SwapResponseBuilder(pageModel);
    }

    public SwapResponseBuilder Event(EventKey eventKey, object? payload = null)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        
        _logger.EventTriggered(eventKey.Name, payload?.GetType().Name ?? "null");
        
        // Ensure the event is recorded on the bus so ResolveChains can find it and its downstream events
        _eventBus.Emit(eventKey, payload);
        
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
        
        _logger.EventTriggered(eventKey.Name, payload?.GetType().Name ?? "null");
        
        // Ensure the event is recorded on the bus so ResolveChains can find it and its downstream events
        _eventBus.Emit(eventKey, payload);
        
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

    public SwapResponseBuilder Event(EventKey eventKey, PageModel pageModel, object? payload = null)
    {
        var httpContext = pageModel.HttpContext;
        
        _logger.EventTriggered(eventKey.Name, payload?.GetType().Name ?? "null");
        
        var result = _executor.Execute(eventKey, httpContext, pageModel, payload);
        
        if (result != null)
        {
            if (payload != null)
            {
                result.WithTrigger(eventKey, payload);
            }
            return result;
        }

        // No chain configured - return empty builder
        var builder = Response(pageModel);
        
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }

    public async Task<SwapResponseBuilder> EventAsync(EventKey eventKey, object? payload = null)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        
        _logger.EventTriggered(eventKey.Name, payload?.GetType().Name ?? "null");
        
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
        
        _logger.EventTriggered(eventKey.Name, payload?.GetType().Name ?? "null");
        
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

    public async Task<SwapResponseBuilder> EventAsync(EventKey eventKey, PageModel pageModel, object? payload = null)
    {
        var httpContext = pageModel.HttpContext;
        
        _logger.EventTriggered(eventKey.Name, payload?.GetType().Name ?? "null");
        
        var result = await _executor.ExecuteAsync(eventKey, httpContext, pageModel, payload);
        
        if (result != null)
        {
            if (payload != null)
            {
                result.WithTrigger(eventKey, payload);
            }
            return result;
        }

        // No chain configured - return empty builder
        var builder = Response(pageModel);
        
        if (payload != null)
        {
            builder.WithTrigger(eventKey, payload);
        }
        
        return builder;
    }
}
