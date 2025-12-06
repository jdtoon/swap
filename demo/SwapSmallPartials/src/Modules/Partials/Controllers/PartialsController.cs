using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Partials.Events;
using SwapSmallPartials.Modules.Partials.Events.Handlers;

namespace SwapSmallPartials.Modules.Partials.Controllers;

[Route("partials")]
public class PartialsController : SwapController
{
    private readonly PartialsState _state;
    
    public PartialsController(PartialsState state)
    {
        _state = state;
    }
    
    /// <summary>
    /// Main page with all 25 partials displayed.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return SwapView(_state);
    }
    
    // =============================================================================
    // COUNTER ACTIONS (10 counters)
    // Each click fires an event that updates:
    // - The specific counter partial
    // - Total, Average, Max, Min partials (via handlers)
    // =============================================================================
    
    [HttpPost("counter/{number:int}/increment")]
    public IActionResult IncrementCounter(int number)
    {
        if (number < 1 || number > 10)
            return BadRequest();
            
        _state.IncrementCounter(number);
        
        // Fire the event using EventKey + typed payload
        return SwapEvent(PartialsEvents.Partial.Counter1.Incremented, new CounterIncrementedEvent(number))
            .WithInfoToast($"Counter {number} incremented!")
            .Build();
    }
    
    // =============================================================================
    // STATUS ACTIONS (5 statuses)
    // Each toggle fires an event that updates:
    // - The specific status partial
    // - Active Statuses count partial (via handler)
    // =============================================================================
    
    [HttpPost("status/{number:int}/toggle")]
    public IActionResult ToggleStatus(int number)
    {
        if (number < 1 || number > 5)
            return BadRequest();
            
        _state.ToggleStatus(number);
        
        return SwapEvent(PartialsEvents.Partial.Status1.Toggled, new StatusToggledEvent(number))
            .WithInfoToast($"Status {number} toggled!")
            .Build();
    }
    
    // =============================================================================
    // PROGRESS ACTIONS (5 progress bars)
    // Each update fires an event that updates the specific progress partial
    // =============================================================================
    
    [HttpPost("progress/{number:int}/increase")]
    public IActionResult IncreaseProgress(int number)
    {
        if (number < 1 || number > 5)
            return BadRequest();
            
        _state.UpdateProgress(number, 10);
        
        return SwapEvent(PartialsEvents.Partial.Progress1.Updated, new ProgressUpdatedEvent(number))
            .WithInfoToast($"Progress {number} increased!")
            .Build();
    }
    
    [HttpPost("progress/{number:int}/decrease")]
    public IActionResult DecreaseProgress(int number)
    {
        if (number < 1 || number > 5)
            return BadRequest();
            
        _state.UpdateProgress(number, -10);
        
        return SwapEvent(PartialsEvents.Partial.Progress1.Updated, new ProgressUpdatedEvent(number))
            .WithInfoToast($"Progress {number} decreased!")
            .Build();
    }
    
    // =============================================================================
    // BULK ACTIONS
    // Reset fires an event that updates ALL 25 partials at once!
    // =============================================================================
    
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _state.Reset();
        
        return SwapEvent(PartialsEvents.Partial.All.Reset, new AllResetEvent())
            .WithWarningToast("All partials reset!")
            .Build();
    }
    
    /// <summary>
    /// Increment ALL counters at once - fires 10 events!
    /// This demonstrates the system handling many concurrent updates.
    /// </summary>
    [HttpPost("increment-all")]
    public IActionResult IncrementAll()
    {
        // Increment all counters
        for (int i = 1; i <= 10; i++)
        {
            _state.IncrementCounter(i);
        }
        
        // Fire events for all counters - this will trigger:
        // - 10 counter updates
        // - 10 total updates (deduplicated to 1)
        // - 10 average updates (deduplicated to 1)
        // - 10 max updates (deduplicated to 1)
        // - 10 min updates (deduplicated to 1)
        var builder = SwapResponse();
        
        for (int i = 1; i <= 10; i++)
        {
            builder.AlsoUpdate($"partial-counter-{i}", "_Counter", 
                new CounterModel { Number = i, Value = _state.GetCounter(i) });
        }
        
        // Update aggregates
        builder.AlsoUpdate("partial-total", "_Total", _state);
        builder.AlsoUpdate("partial-average", "_Average", _state);
        builder.AlsoUpdate("partial-max", "_Max", _state);
        builder.AlsoUpdate("partial-min", "_Min", _state);
        
        return builder.WithSuccessToast("All 10 counters incremented! (14 partials updated)")
            .Build();
    }
    
    /// <summary>
    /// Toggle ALL statuses at once - fires 5 events!
    /// </summary>
    [HttpPost("toggle-all")]
    public IActionResult ToggleAll()
    {
        for (int i = 1; i <= 5; i++)
        {
            _state.ToggleStatus(i);
        }
        
        var builder = SwapResponse();
        
        for (int i = 1; i <= 5; i++)
        {
            builder.AlsoUpdate($"partial-status-{i}", "_Status", 
                new StatusModel { Number = i, IsActive = _state.GetStatus(i) });
        }
        
        builder.AlsoUpdate("partial-active-statuses", "_ActiveStatuses", _state);
        
        return builder.WithSuccessToast("All 5 statuses toggled! (6 partials updated)")
            .Build();
    }
    
    /// <summary>
    /// Update ALL progress bars at once - fires 5 events!
    /// </summary>
    [HttpPost("progress-all")]
    public IActionResult ProgressAll()
    {
        for (int i = 1; i <= 5; i++)
        {
            _state.UpdateProgress(i, 5);
        }
        
        var builder = SwapResponse();
        
        for (int i = 1; i <= 5; i++)
        {
            builder.AlsoUpdate($"partial-progress-{i}", "_Progress", 
                new ProgressModel { Number = i, Value = _state.GetProgress(i) });
        }
        
        return builder.WithSuccessToast("All 5 progress bars updated!")
            .Build();
    }
}
