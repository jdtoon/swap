using Swap.Htmx.Attributes;

namespace SwapModularMonolith.Modules.Notes.Events;

/// <summary>
/// Type-safe event keys for the Notes module.
/// The [SwapEventSource] attribute triggers code generation that creates
/// a hierarchy based on the event name parts.
/// Example: "notes.listChanged" generates NotesEvents.Notes.ListChanged
/// </summary>
[SwapEventSource]
public static partial class NotesEvents
{
    // Server triggers this → Client listens to refresh the list
    public const string NotesListChanged = "notes.listChanged";
}
