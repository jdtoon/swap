using Microsoft.EntityFrameworkCore;

namespace Swap.Patterns.Orderable;

/// <summary>
/// Extension methods for working with orderable entities.
/// </summary>
public static class OrderableExtensions
{
    /// <summary>
    /// Orders the query by Position ascending (lowest first).
    /// </summary>
    public static IQueryable<T> OrderByPosition<T>(this IQueryable<T> query) where T : IOrderable
    {
        return query.OrderBy(e => e.Position);
    }

    /// <summary>
    /// Orders the query by Position descending (highest first).
    /// </summary>
    public static IQueryable<T> OrderByPositionDescending<T>(this IQueryable<T> query) where T : IOrderable
    {
        return query.OrderByDescending(e => e.Position);
    }

    /// <summary>
    /// Moves an entity to a new position, reordering other entities as needed.
    /// Call SaveChanges after this to persist the changes.
    /// </summary>
    /// <param name="dbSet">The DbSet containing the entities</param>
    /// <param name="entity">The entity to move</param>
    /// <param name="newPosition">The new position (1-based)</param>
    public static async Task ReorderAsync<T>(
        this DbSet<T> dbSet,
        T entity,
        int newPosition) where T : class, IOrderable
    {
        var oldPosition = entity.Position;
        if (oldPosition == newPosition) return;

        if (newPosition < oldPosition)
        {
            // Moving up - shift items down
            var itemsToShift = await dbSet
                .Where(e => e.Position >= newPosition && e.Position < oldPosition)
                .ToListAsync();

            foreach (var item in itemsToShift)
            {
                item.Position++;
            }
        }
        else
        {
            // Moving down - shift items up
            var itemsToShift = await dbSet
                .Where(e => e.Position > oldPosition && e.Position <= newPosition)
                .ToListAsync();

            foreach (var item in itemsToShift)
            {
                item.Position--;
            }
        }

        entity.Position = newPosition;
    }

    /// <summary>
    /// Normalizes positions to ensure they are sequential starting from 1.
    /// Useful after deletions or bulk operations.
    /// Call SaveChanges after this to persist the changes.
    /// </summary>
    public static async Task NormalizePositionsAsync<T>(this DbSet<T> dbSet) where T : class, IOrderable
    {
        var items = await dbSet.OrderBy(e => e.Position).ToListAsync();
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Position = i + 1;
        }
    }

    /// <summary>
    /// Gets the next available position for a new entity.
    /// Returns max position + 1, or 1 if no entities exist.
    /// </summary>
    public static async Task<int> GetNextPositionAsync<T>(this DbSet<T> dbSet) where T : class, IOrderable
    {
        var maxPosition = await dbSet.MaxAsync(e => (int?)e.Position);
        return (maxPosition ?? 0) + 1;
    }
}
