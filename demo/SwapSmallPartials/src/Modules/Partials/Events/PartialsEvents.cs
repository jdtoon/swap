using Swap.Htmx.Attributes;

namespace SwapSmallPartials.Modules.Partials.Events;

/// <summary>
/// Events for the 25 small partials.
/// Each action can trigger updates to multiple other partials.
/// </summary>
[SwapEventSource]
public static partial class PartialsEvents
{
    // Counter events (10 counters)
    public const string Counter1Incremented = "partial.counter1.incremented";
    public const string Counter2Incremented = "partial.counter2.incremented";
    public const string Counter3Incremented = "partial.counter3.incremented";
    public const string Counter4Incremented = "partial.counter4.incremented";
    public const string Counter5Incremented = "partial.counter5.incremented";
    public const string Counter6Incremented = "partial.counter6.incremented";
    public const string Counter7Incremented = "partial.counter7.incremented";
    public const string Counter8Incremented = "partial.counter8.incremented";
    public const string Counter9Incremented = "partial.counter9.incremented";
    public const string Counter10Incremented = "partial.counter10.incremented";
    
    // Status toggle events (5 statuses)
    public const string Status1Toggled = "partial.status1.toggled";
    public const string Status2Toggled = "partial.status2.toggled";
    public const string Status3Toggled = "partial.status3.toggled";
    public const string Status4Toggled = "partial.status4.toggled";
    public const string Status5Toggled = "partial.status5.toggled";
    
    // Progress events (5 progress bars)
    public const string Progress1Updated = "partial.progress1.updated";
    public const string Progress2Updated = "partial.progress2.updated";
    public const string Progress3Updated = "partial.progress3.updated";
    public const string Progress4Updated = "partial.progress4.updated";
    public const string Progress5Updated = "partial.progress5.updated";
    
    // Aggregate events (triggered when computed values need update)
    public const string TotalUpdated = "partial.total.updated";
    public const string AverageUpdated = "partial.average.updated";
    public const string MaxUpdated = "partial.max.updated";
    public const string MinUpdated = "partial.min.updated";
    public const string ActiveStatusesUpdated = "partial.activestatuses.updated";
    
    // Global events
    public const string AllReset = "partial.all.reset";
    public const string AnyCounterChanged = "partial.counter.any";
    public const string AnyStatusChanged = "partial.status.any";
    public const string AnyProgressChanged = "partial.progress.any";
}
