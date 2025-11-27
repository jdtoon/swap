using Microsoft.Extensions.Logging;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace Swap.Htmx.Diagnostics;

/// <summary>
/// Development-time diagnostics for Swap.Htmx.
/// Provides warnings for common issues like unhandled events and missing OOB targets.
/// </summary>
public interface ISwapDiagnostics
{
    /// <summary>
    /// Validates a SwapResponseBuilder and logs any warnings.
    /// </summary>
    void ValidateResponse(SwapResponseBuilder builder);
    
    /// <summary>
    /// Checks if an event has any handlers configured.
    /// </summary>
    bool HasEventHandlers(string eventName);
    
    /// <summary>
    /// Logs a warning if the event has no handlers.
    /// </summary>
    void WarnIfUnhandledEvent(string eventName);
}

/// <summary>
/// Default implementation of Swap diagnostics.
/// </summary>
internal class SwapDiagnostics : ISwapDiagnostics
{
    private readonly SwapHtmxOptions _options;
    private readonly SwapEventBusOptions _eventBusOptions;
    private readonly SwapEventHandlerRegistry _handlerRegistry;
    private readonly ILogger<SwapDiagnostics> _logger;
    private readonly HashSet<string> _warnedEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warnedTargets = new(StringComparer.OrdinalIgnoreCase);

    public SwapDiagnostics(
        SwapHtmxOptions options,
        SwapEventBusOptions eventBusOptions,
        SwapEventHandlerRegistry handlerRegistry,
        ILogger<SwapDiagnostics> logger)
    {
        _options = options;
        _eventBusOptions = eventBusOptions;
        _handlerRegistry = handlerRegistry;
        _logger = logger;
    }

    public void ValidateResponse(SwapResponseBuilder builder)
    {
        if (!_options.Diagnostics.WarnOnUnhandledEvents && 
            !_options.Diagnostics.WarnOnMissingOobTargets)
        {
            return;
        }

        // Check triggers for unhandled events
        if (_options.Diagnostics.WarnOnUnhandledEvents)
        {
            foreach (var trigger in builder.Triggers)
            {
                WarnIfUnhandledEvent(trigger.EventName);
            }
        }

        // Check OOB targets
        if (_options.Diagnostics.WarnOnMissingOobTargets)
        {
            foreach (var oob in builder.OobSwaps)
            {
                WarnIfSuspiciousTarget(oob.TargetId);
            }
        }
    }

    public bool HasEventHandlers(string eventName)
    {
        // Check if event is in any chain configuration
        var chains = _eventBusOptions.GetChainsSnapshot();
        if (chains.ContainsKey(eventName))
        {
            return true;
        }

        // Check if any chain triggers this event
        foreach (var chain in chains.Values)
        {
            if (chain.Contains(eventName))
            {
                return true;
            }
        }

        // Note: We can't easily check for hx-trigger listeners in HTML from server-side.
        // This check is primarily for server-side event chains.
        return false;
    }

    public void WarnIfUnhandledEvent(string eventName)
    {
        if (!_options.Diagnostics.WarnOnUnhandledEvents)
        {
            return;
        }

        // Skip built-in events
        if (eventName.StartsWith("showToast", StringComparison.OrdinalIgnoreCase) ||
            eventName.StartsWith("validationFailed", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Only warn once per event name per application lifetime
        if (!_warnedEvents.Add(eventName))
        {
            return;
        }

        if (!HasEventHandlers(eventName))
        {
            _logger.LogWarning(
                "[Swap.Htmx] Event '{EventName}' was triggered but no server-side handlers are configured. " +
                "If you're using hx-trigger in HTML to listen for this event, this warning can be ignored. " +
                "Otherwise, ensure you have an ISwapEventConfiguration or hx-trigger listening for this event.",
                eventName);
        }
    }

    private void WarnIfSuspiciousTarget(string targetId)
    {
        if (!_options.Diagnostics.WarnOnMissingOobTargets)
        {
            return;
        }

        // Only warn once per target per application lifetime
        if (!_warnedTargets.Add(targetId))
        {
            return;
        }

        // Warn about common mistakes
        if (string.IsNullOrWhiteSpace(targetId))
        {
            _logger.LogWarning(
                "[Swap.Htmx] OOB swap has empty target ID. This will silently fail.");
            return;
        }

        if (targetId.StartsWith("#"))
        {
            _logger.LogWarning(
                "[Swap.Htmx] OOB target '{TargetId}' includes '#' prefix. " +
                "AlsoUpdate() expects just the ID without '#'. Use '{CorrectId}' instead.",
                targetId, targetId.TrimStart('#'));
        }

        if (targetId.Contains(" "))
        {
            _logger.LogWarning(
                "[Swap.Htmx] OOB target '{TargetId}' contains spaces. " +
                "Element IDs should not contain spaces.",
                targetId);
        }
    }
}

/// <summary>
/// No-op diagnostics implementation for production.
/// </summary>
public class NullSwapDiagnostics : ISwapDiagnostics
{
    public static readonly NullSwapDiagnostics Instance = new();
    
    public void ValidateResponse(SwapResponseBuilder builder) { }
    public bool HasEventHandlers(string eventName) => true;
    public void WarnIfUnhandledEvent(string eventName) { }
}
