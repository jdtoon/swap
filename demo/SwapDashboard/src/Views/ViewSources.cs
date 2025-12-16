using Swap.Htmx.Attributes;

namespace SwapDashboard.Views;

/// <summary>
/// Generated view path constants for the Dashboard folder.
/// The [SwapViewSource] attribute triggers the source generator to scan
/// Views/Dashboard for .cshtml files and generate constants.
/// </summary>
/// <remarks>
/// Usage in controller/handler:
/// <code>
/// builder.AlsoUpdate(DashboardIds.StatsPanel, DashboardViews.Partials.StatsPanel, stats);
/// </code>
/// 
/// Instead of magic strings:
/// <code>
/// builder.AlsoUpdate("stats-panel", "_StatsPanel", stats);  // Error prone!
/// </code>
/// </remarks>
[SwapViewSource("Views/Dashboard")]
public static partial class DashboardViews { }

/// <summary>
/// Generated view path constants for the Shared folder.
/// </summary>
[SwapViewSource("Views/Shared")]
public static partial class SharedViews { }

/// <summary>
/// Generated element ID constants for the Dashboard folder.
/// The [SwapElementSource] attribute scans Views/Dashboard/*.cshtml files
/// for id="..." attributes and generates strongly-typed constants.
/// </summary>
/// <remarks>
/// Usage in handler:
/// <code>
/// builder.AlsoUpdate(DashboardIds.StatsPanel, DashboardViews.Partials.StatsPanel, stats);
/// builder.AlsoUpdate(DashboardIds.KanbanTodo, DashboardViews.Partials.KanbanColumn, todoModel);
/// </code>
/// </remarks>
[SwapElementSource("Views/Dashboard")]
public static partial class DashboardIds { }
