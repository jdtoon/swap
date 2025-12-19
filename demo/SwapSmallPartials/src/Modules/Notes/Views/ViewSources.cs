using Swap.Htmx.Attributes;

namespace SwapSmallPartials.Modules.Notes.Views;

/// <summary>
/// Generated view path constants for the Notes module.
/// The [SwapViewSource] attribute triggers the source generator to scan
/// Modules/Notes/Views for .cshtml files and generate constants.
/// </summary>
/// <remarks>
/// Usage in controller:
/// <code>
/// return SwapView(NotesViews.Partials.NotesList, notes);
/// </code>
/// </remarks>
[SwapViewSource("Modules/Notes/Views")]
public static partial class NotesViews { }

/// <summary>
/// Generated element ID constants for the Notes module.
/// </summary>
[SwapElementSource("Modules/Notes/Views")]
public static partial class NotesIds { }
