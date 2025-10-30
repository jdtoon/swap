namespace Swap.Patterns.Versionable;

/// <summary>
/// Query helpers for versionable entities.
/// </summary>
public static class VersionableExtensions
{
    /// <summary>
    /// Filters to entities with version greater than or equal to the given value.
    /// </summary>
    public static IQueryable<T> WithMinVersion<T>(this IQueryable<T> query, int minVersion)
        where T : class, IVersionable
        => query.Where(e => e.Version >= minVersion);

    /// <summary>
    /// Filters to entities with the exact version.
    /// </summary>
    public static IQueryable<T> WithVersion<T>(this IQueryable<T> query, int version)
        where T : class, IVersionable
        => query.Where(e => e.Version == version);

    /// <summary>
    /// Orders entities by Version ascending.
    /// </summary>
    public static IQueryable<T> OrderByVersion<T>(this IQueryable<T> query)
        where T : class, IVersionable
        => query.OrderBy(e => e.Version);

    /// <summary>
    /// Orders entities by Version descending.
    /// </summary>
    public static IQueryable<T> OrderByVersionDescending<T>(this IQueryable<T> query)
        where T : class, IVersionable
        => query.OrderByDescending(e => e.Version);
}
