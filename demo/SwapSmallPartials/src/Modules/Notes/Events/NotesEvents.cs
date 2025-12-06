using Swap.Htmx.Attributes;

namespace SwapSmallPartials.Modules.Notes.Events;

/// <summary>
/// Type-safe event keys for the Notes module.
/// The source generator creates a hierarchy based on the event name parts.
/// Example: "notes.created" generates NotesEvents.Notes.Created
/// </summary>
[SwapEventSource]
public static partial class NotesEvents
{
    // Note lifecycle events (generates NotesEvents.Notes.Created, .Updated, etc.)
    public const string NotesCreated = "notes.created";
    public const string NotesUpdated = "notes.updated";
    public const string NotesDeleted = "notes.deleted";
    public const string NotesPinned = "notes.pinned";

    // List refresh trigger (generates NotesEvents.Notes.ListChanged)
    public const string NotesListChanged = "notes.listChanged";
}
