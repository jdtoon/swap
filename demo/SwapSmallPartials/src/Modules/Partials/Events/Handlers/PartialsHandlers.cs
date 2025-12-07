using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace SwapSmallPartials.Modules.Partials.Events.Handlers;

/// <summary>
/// Handler for Total partial - updates when any counter changes.
/// </summary>
[SwapHandler]
public class TotalHandler : ISwapEventHandler<CounterIncrementedEvent>
{
    private readonly PartialsState _state;
    
    public TotalHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(CounterIncrementedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-total", "_Total", _state);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for Average partial - updates when any counter changes.
/// </summary>
[SwapHandler]
public class AverageHandler : ISwapEventHandler<CounterIncrementedEvent>
{
    private readonly PartialsState _state;
    
    public AverageHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(CounterIncrementedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-average", "_Average", _state);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for Max partial - updates when any counter changes.
/// </summary>
[SwapHandler]
public class MaxHandler : ISwapEventHandler<CounterIncrementedEvent>
{
    private readonly PartialsState _state;
    
    public MaxHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(CounterIncrementedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-max", "_Max", _state);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for Min partial - updates when any counter changes.
/// </summary>
[SwapHandler]
public class MinHandler : ISwapEventHandler<CounterIncrementedEvent>
{
    private readonly PartialsState _state;
    
    public MinHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(CounterIncrementedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-min", "_Min", _state);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for the individual counter that was incremented.
/// </summary>
[SwapHandler]
public class CounterHandler : ISwapEventHandler<CounterIncrementedEvent>
{
    private readonly PartialsState _state;
    
    public CounterHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(CounterIncrementedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Update the specific counter partial
        builder.AlsoUpdate($"partial-counter-{e.CounterNumber}", "_Counter", 
            new CounterModel { Number = e.CounterNumber, Value = _state.GetCounter(e.CounterNumber) });
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for Active Statuses count - updates when any status toggles.
/// </summary>
[SwapHandler]
public class ActiveStatusesHandler : ISwapEventHandler<StatusToggledEvent>
{
    private readonly PartialsState _state;
    
    public ActiveStatusesHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(StatusToggledEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-active-statuses", "_ActiveStatuses", _state);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for the individual status that was toggled.
/// </summary>
[SwapHandler]
public class StatusHandler : ISwapEventHandler<StatusToggledEvent>
{
    private readonly PartialsState _state;
    
    public StatusHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(StatusToggledEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate($"partial-status-{e.StatusNumber}", "_Status", 
            new StatusModel { Number = e.StatusNumber, IsActive = _state.GetStatus(e.StatusNumber) });
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for the individual progress bar that was updated.
/// </summary>
[SwapHandler]
public class ProgressHandler : ISwapEventHandler<ProgressUpdatedEvent>
{
    private readonly PartialsState _state;
    
    public ProgressHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(ProgressUpdatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate($"partial-progress-{e.ProgressNumber}", "_Progress", 
            new ProgressModel { Number = e.ProgressNumber, Value = _state.GetProgress(e.ProgressNumber) });
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for reset - updates ALL partials.
/// </summary>
[SwapHandler]
public class ResetHandler : ISwapEventHandler<AllResetEvent>
{
    private readonly PartialsState _state;
    
    public ResetHandler(PartialsState state) => _state = state;
    
    public Task HandleAsync(AllResetEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Update all counters
        for (int i = 1; i <= 10; i++)
        {
            builder.AlsoUpdate($"partial-counter-{i}", "_Counter", 
                new CounterModel { Number = i, Value = _state.GetCounter(i) });
        }
        
        // Update all statuses
        for (int i = 1; i <= 5; i++)
        {
            builder.AlsoUpdate($"partial-status-{i}", "_Status", 
                new StatusModel { Number = i, IsActive = _state.GetStatus(i) });
        }
        
        // Update all progress bars
        for (int i = 1; i <= 5; i++)
        {
            builder.AlsoUpdate($"partial-progress-{i}", "_Progress", 
                new ProgressModel { Number = i, Value = _state.GetProgress(i) });
        }
        
        // Update aggregates
        builder.AlsoUpdate("partial-total", "_Total", _state);
        builder.AlsoUpdate("partial-average", "_Average", _state);
        builder.AlsoUpdate("partial-max", "_Max", _state);
        builder.AlsoUpdate("partial-min", "_Min", _state);
        builder.AlsoUpdate("partial-active-statuses", "_ActiveStatuses", _state);
        
        return Task.CompletedTask;
    }
}

// =============================================================================
// EVENT CLASSES
// =============================================================================

public record CounterIncrementedEvent(int CounterNumber);
public record StatusToggledEvent(int StatusNumber);
public record ProgressUpdatedEvent(int ProgressNumber);
public record AllResetEvent;

// =============================================================================
// VIEW MODELS
// =============================================================================

public record CounterModel
{
    public int Number { get; init; }
    public int Value { get; init; }
}

public record StatusModel
{
    public int Number { get; init; }
    public bool IsActive { get; init; }
}

public record ProgressModel
{
    public int Number { get; init; }
    public int Value { get; init; }
}
