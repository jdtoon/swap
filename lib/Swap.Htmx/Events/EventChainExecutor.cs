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
    /// <param name="controller">The controller handling the request.</param>
    /// <returns>A SwapResponseBuilder configured with all event chain actions, or null if no chain exists.</returns>
    SwapResponseBuilder? Execute(EventKey eventKey, HttpContext httpContext, ControllerBase controller);
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

    public SwapResponseBuilder? Execute(EventKey eventKey, HttpContext httpContext, ControllerBase controller)
    {
        var configs = _options.GetEventChainConfigs();
        var logger = httpContext.RequestServices.GetService<ILogger<EventChainExecutor>>();
        
        if (!configs.TryGetValue(eventKey.Name, out var config))
        {
            // No chain configured for this event
            Dev.SwapDevLogger.LogSwapEvent(logger, eventKey.Name, "No chain configured");
            return null;
        }

        Dev.SwapDevLogger.LogEventChain(logger, eventKey.Name, config.Partials.Count, config.Toasts.Count);

        var builder = new SwapResponseBuilder
        {
            Controller = controller as Controller
        };

        // Add configured partials
        foreach (var partial in config.Partials)
        {
            var model = partial.ModelFactory?.Invoke(httpContext);
            builder.AlsoUpdate(partial.TargetId, partial.ViewName, model, partial.SwapMode);
        }

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

        return builder;
    }
}
