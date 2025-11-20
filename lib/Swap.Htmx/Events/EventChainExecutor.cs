using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;

namespace Swap.Htmx.Events;

/// <summary>
/// Service that executes event chains and builds coordinated responses.
/// </summary>
public interface IEventChainExecutor
{
    /// <summary>
    /// Executes the event chain for a given event and builds a SwapResponseBuilder with all configured actions.
    /// </summary>
    /// <param name="eventKey">The event that was triggered.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="controller">The controller handling the request (optional).</param>
    /// <param name="payload">Optional event payload to pass to model factories.</param>
    /// <returns>A SwapResponseBuilder configured with all event chain actions, or null if no chain exists.</returns>
    SwapResponseBuilder? Execute(EventKey eventKey, HttpContext httpContext, ControllerBase? controller, object? payload = null);

    /// <summary>
    /// Asynchronously executes the event chain for a given event and builds a SwapResponseBuilder with all configured actions.
    /// </summary>
    /// <param name="eventKey">The event that was triggered.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="controller">The controller handling the request (optional).</param>
    /// <param name="payload">Optional event payload to pass to model factories.</param>
    /// <returns>A SwapResponseBuilder configured with all event chain actions, or null if no chain exists.</returns>
    Task<SwapResponseBuilder?> ExecuteAsync(EventKey eventKey, HttpContext httpContext, ControllerBase? controller, object? payload = null);
}

/// <summary>
/// Default implementation of event chain executor.
/// </summary>
internal sealed class EventChainExecutor : IEventChainExecutor
{
    private readonly SwapEventBusOptions _options;

    public EventChainExecutor(SwapEventBusOptions options)
    {
        _options = options;
    }

    public SwapResponseBuilder? Execute(EventKey eventKey, HttpContext httpContext, ControllerBase? controller, object? payload = null)
    {
        // For backward compatibility, we call the async version and block.
        // This is not ideal but necessary until we fully migrate to async.
        // However, since we are introducing async factories, calling them synchronously is dangerous.
        // If the user configured an async factory, this method will throw or deadlock if we just .Result it.
        // But wait, we can't easily call async code from sync.
        // We should implement the sync logic separately to avoid overhead if possible, 
        // OR just implement the sync logic as it was, and throw if async factory is encountered.
        
        var configs = _options.GetEventChainConfigs();
        var logger = httpContext.RequestServices?.GetService<ILogger<EventChainExecutor>>();
        
        if (!configs.TryGetValue(eventKey.Name, out var config))
        {
            Dev.SwapDevLogger.LogSwapEvent(logger, eventKey.Name, "No chain configured");
            return null;
        }

        Dev.SwapDevLogger.LogEventChain(logger, eventKey.Name, config.Partials.Count, config.Toasts.Count);

        var builder = new SwapResponseBuilder
        {
            Controller = controller as Controller
        };

        foreach (var partial in config.Partials)
        {
            if (partial.ModelFactoryAsync != null || partial.ModelFactoryWithPayloadAsync != null)
            {
                throw new InvalidOperationException($"Event chain for '{eventKey.Name}' contains async model factories but was triggered synchronously. Use SwapEventAsync() instead.");
            }

            var model = partial.ModelFactoryWithPayload != null
                ? partial.ModelFactoryWithPayload(httpContext, payload)
                : partial.ModelFactory?.Invoke(httpContext);
                
            builder.AlsoUpdate(partial.TargetId, partial.ViewName, model, partial.SwapMode);
        }

        ApplyCommonActions(builder, config);
        return builder;
    }

    public async Task<SwapResponseBuilder?> ExecuteAsync(EventKey eventKey, HttpContext httpContext, ControllerBase? controller, object? payload = null)
    {
        var configs = _options.GetEventChainConfigs();
        var logger = httpContext.RequestServices?.GetService<ILogger<EventChainExecutor>>();
        
        if (!configs.TryGetValue(eventKey.Name, out var config))
        {
            Dev.SwapDevLogger.LogSwapEvent(logger, eventKey.Name, "No chain configured");
            return null;
        }

        Dev.SwapDevLogger.LogEventChain(logger, eventKey.Name, config.Partials.Count, config.Toasts.Count);

        var builder = new SwapResponseBuilder
        {
            Controller = controller as Controller
        };

        foreach (var partial in config.Partials)
        {
            object? model = null;

            if (partial.ModelFactoryWithPayloadAsync != null)
            {
                model = await partial.ModelFactoryWithPayloadAsync(httpContext, payload);
            }
            else if (partial.ModelFactoryAsync != null)
            {
                model = await partial.ModelFactoryAsync(httpContext);
            }
            else if (partial.ModelFactoryWithPayload != null)
            {
                model = partial.ModelFactoryWithPayload(httpContext, payload);
            }
            else if (partial.ModelFactory != null)
            {
                model = partial.ModelFactory(httpContext);
            }

            builder.AlsoUpdate(partial.TargetId, partial.ViewName, model, partial.SwapMode);
        }

        ApplyCommonActions(builder, config);
        return builder;
    }

    private void ApplyCommonActions(SwapResponseBuilder builder, EventChainConfiguration config)
    {
        // Add configured toasts
        foreach (var toast in config.Toasts)
        {
            builder = toast.Type switch
            {
                ToastType.Success => builder.WithSuccessToast(toast.Message),
                ToastType.Error => builder.WithErrorToast(toast.Message),
                ToastType.Warning => builder.WithWarningToast(toast.Message),
                ToastType.Info => builder.WithInfoToast(toast.Message),
                _ => builder.WithInfoToast(toast.Message)
            };
        }

        // Add trigger events
        foreach (var triggerEvent in config.TriggerEvents)
        {
            builder.WithTrigger(triggerEvent);
        }

        // Add redirect if configured
        if (config.Redirect != null)
        {
            builder.WithRedirect(config.Redirect.Url);
        }
    }
}
