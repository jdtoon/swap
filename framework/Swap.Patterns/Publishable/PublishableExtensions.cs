using Microsoft.EntityFrameworkCore;

namespace Swap.Patterns.Publishable;

/// <summary>
/// Extension methods for working with publishable entities.
/// </summary>
public static class PublishableExtensions
{
    /// <summary>
    /// Marks an entity as published and sets PublishedAt (UTC) if not provided.
    /// </summary>
    public static void Publish(this IPublishable entity, DateTime? publishedAtUtc = null)
    {
        entity.IsPublished = true;
        entity.PublishedAt = publishedAtUtc ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Marks an entity as draft/unpublished and clears PublishedAt.
    /// </summary>
    public static void Unpublish(this IPublishable entity)
    {
        entity.IsPublished = false;
        entity.PublishedAt = null;
    }

    /// <summary>
    /// Filters only published entities.
    /// </summary>
    public static IQueryable<T> Published<T>(this IQueryable<T> query) where T : class, IPublishable
        => query.Where(e => e.IsPublished);

    /// <summary>
    /// Filters only draft/unpublished entities.
    /// </summary>
    public static IQueryable<T> Drafts<T>(this IQueryable<T> query) where T : class, IPublishable
        => query.Where(e => !e.IsPublished);

    /// <summary>
    /// Filters published entities published after the specified UTC time.
    /// </summary>
    public static IQueryable<T> PublishedAfter<T>(this IQueryable<T> query, DateTime utc)
        where T : class, IPublishable
        => query.Where(e => e.IsPublished && e.PublishedAt != null && e.PublishedAt > utc);

    /// <summary>
    /// Filters published entities published before the specified UTC time.
    /// </summary>
    public static IQueryable<T> PublishedBefore<T>(this IQueryable<T> query, DateTime utc)
        where T : class, IPublishable
        => query.Where(e => e.IsPublished && e.PublishedAt != null && e.PublishedAt < utc);
}
