using Microsoft.EntityFrameworkCore;

namespace Swap.Patterns.SoftDelete;

/// <summary>
/// Extension methods for soft delete operations on entities and DbContext.
/// </summary>
public static class SoftDeleteExtensions
{
    /// <summary>
    /// Soft deletes an entity by marking it as deleted.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ISoftDeletable</typeparam>
    /// <param name="entity">The entity to soft delete</param>
    /// <param name="deletedBy">Optional: User or system performing the deletion</param>
    public static void SoftDelete<T>(this T entity, string? deletedBy = null) where T : ISoftDeletable
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ISoftDeletable</typeparam>
    /// <param name="entity">The entity to restore</param>
    public static void Restore<T>(this T entity) where T : ISoftDeletable
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
    }

    /// <summary>
    /// Configures a global query filter on the DbContext to exclude soft-deleted entities.
    /// Call this in your DbContext's OnModelCreating method.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ConfigureSoftDeleteFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, parameter);

                entityType.SetQueryFilter(lambda);
            }
        }
    }

    /// <summary>
    /// Returns a queryable that includes soft-deleted entities.
    /// Use this when you need to query deleted entities explicitly.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ISoftDeletable</typeparam>
    /// <param name="query">The query to include deleted entities</param>
    /// <returns>Query including soft-deleted entities</returns>
    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) where T : class, ISoftDeletable
    {
        return query.IgnoreQueryFilters();
    }

    /// <summary>
    /// Returns a queryable containing only soft-deleted entities.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ISoftDeletable</typeparam>
    /// <param name="query">The query</param>
    /// <returns>Query with only soft-deleted entities</returns>
    public static IQueryable<T> OnlyDeleted<T>(this IQueryable<T> query) where T : class, ISoftDeletable
    {
        return query.IgnoreQueryFilters().Where(e => e.IsDeleted);
    }
}
