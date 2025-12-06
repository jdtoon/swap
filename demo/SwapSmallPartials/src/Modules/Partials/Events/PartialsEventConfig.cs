using Swap.Htmx;
using Swap.Htmx.Events;

namespace SwapSmallPartials.Modules.Partials.Events;

/// <summary>
/// Centralized event configuration that creates a web of triggers.
/// When ANY counter changes, it triggers updates to:
/// - The total partial
/// - The average partial
/// - The max partial
/// - The min partial
/// - The "any counter changed" aggregate
/// 
/// This creates a cascading effect where one click causes many partials to update.
/// </summary>
public class PartialsEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        // =============================================================================
        // COUNTER EVENTS → Trigger aggregate updates
        // Every counter increment triggers all computed value updates
        // =============================================================================
        
        events.When(PartialsEvents.Partial.Counter1.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter2.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter3.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter4.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter5.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter6.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter7.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter8.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter9.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);
            
        events.When(PartialsEvents.Partial.Counter10.Incremented)
            .AlsoTrigger(PartialsEvents.Partial.Total.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Average.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Max.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Min.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Counter.Any);

        // =============================================================================
        // STATUS EVENTS → Trigger active status count update
        // =============================================================================
        
        events.When(PartialsEvents.Partial.Status1.Toggled)
            .AlsoTrigger(PartialsEvents.Partial.Activestatuses.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Status.Any);
            
        events.When(PartialsEvents.Partial.Status2.Toggled)
            .AlsoTrigger(PartialsEvents.Partial.Activestatuses.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Status.Any);
            
        events.When(PartialsEvents.Partial.Status3.Toggled)
            .AlsoTrigger(PartialsEvents.Partial.Activestatuses.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Status.Any);
            
        events.When(PartialsEvents.Partial.Status4.Toggled)
            .AlsoTrigger(PartialsEvents.Partial.Activestatuses.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Status.Any);
            
        events.When(PartialsEvents.Partial.Status5.Toggled)
            .AlsoTrigger(PartialsEvents.Partial.Activestatuses.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Status.Any);

        // =============================================================================
        // PROGRESS EVENTS → Trigger aggregate progress events
        // =============================================================================
        
        events.When(PartialsEvents.Partial.Progress1.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Progress.Any);
            
        events.When(PartialsEvents.Partial.Progress2.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Progress.Any);
            
        events.When(PartialsEvents.Partial.Progress3.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Progress.Any);
            
        events.When(PartialsEvents.Partial.Progress4.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Progress.Any);
            
        events.When(PartialsEvents.Partial.Progress5.Updated)
            .AlsoTrigger(PartialsEvents.Partial.Progress.Any);
    }
}
