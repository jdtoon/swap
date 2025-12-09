using Swap.Htmx;
using Swap.Htmx.Events;

namespace SwapModularMonolith.Modules.Notes.Events;

/// <summary>
/// Event configuration for the Notes module.
/// This is optional - you can also trigger events directly from controllers
/// or use [SwapHandler] for server-driven OOB updates.
/// </summary>
public class NotesEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        // Example: Additional behavior when list changes
        // This is optional - toasts are already added in the controller
        // events.When(NotesEvents.Notes.ListChanged)
        //     .WithInfoToast("List updated");
    }
}
