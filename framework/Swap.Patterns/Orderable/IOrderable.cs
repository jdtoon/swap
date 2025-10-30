namespace Swap.Patterns.Orderable;

/// <summary>
/// Marks an entity as orderable with manual position/sort order.
/// Useful for drag-and-drop lists, menu items, categories, etc.
/// </summary>
public interface IOrderable
{
    /// <summary>
    /// The position/sort order of the entity.
    /// Lower numbers appear first.
    /// </summary>
    int Position { get; set; }
}
