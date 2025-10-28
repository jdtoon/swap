namespace Swap.Patterns.Visibility;

/// <summary>
/// Marks an entity as having controllable visibility with optional time-based scheduling.
/// Use this for feature flags, scheduled content releases, or time-bound offers.
/// </summary>
public interface IVisibility
{
    /// <summary>
    /// Whether this entity is currently visible/enabled. Manual toggle.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Optional start date/time (UTC) when the entity becomes visible.
    /// Null means no start restriction.
    /// </summary>
    DateTime? VisibleFrom { get; set; }

    /// <summary>
    /// Optional end date/time (UTC) when the entity stops being visible.
    /// Null means no end restriction.
    /// </summary>
    DateTime? VisibleUntil { get; set; }
}
