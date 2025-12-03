using Swap.Htmx;
using Swap.Htmx.Events;

namespace SwapModularMonolith.Modules.Notes.Events;

public class NotesEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        // When any note event occurs, trigger a list refresh
        events.When(NotesEvents.Notes.Created)
            .AlsoTrigger(NotesEvents.Notes.ListChanged);

        events.When(NotesEvents.Notes.Updated)
            .AlsoTrigger(NotesEvents.Notes.ListChanged);

        events.When(NotesEvents.Notes.Deleted)
            .AlsoTrigger(NotesEvents.Notes.ListChanged);

        events.When(NotesEvents.Notes.Pinned)
            .AlsoTrigger(NotesEvents.Notes.ListChanged);
    }
}
