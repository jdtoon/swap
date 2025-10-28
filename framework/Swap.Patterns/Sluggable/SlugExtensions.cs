using Microsoft.EntityFrameworkCore;

namespace Swap.Patterns.Sluggable;

/// <summary>
/// Extension methods for sluggable entities.
/// </summary>
public static class SlugExtensions
{
    /// <summary>
    /// Generates and sets a unique slug for the entity based on the source text.
    /// </summary>
    /// <typeparam name="T">The entity type that implements ISluggable.</typeparam>
    /// <param name="entity">The entity to generate a slug for.</param>
    /// <param name="sourceText">The text to generate the slug from (e.g., title, name).</param>
    /// <param name="dbContext">The DbContext to check for existing slugs.</param>
    /// <param name="maxLength">Maximum length of the slug (default: 80).</param>
    /// <returns>A task representing the async operation.</returns>
    /// <example>
    /// <code>
    /// var post = new BlogPost { Title = "My Awesome Post" };
    /// await post.GenerateSlugAsync(post.Title, db);
    /// // post.Slug is now "my-awesome-post" (or "my-awesome-post-2" if collision)
    /// </code>
    /// </example>
    public static async Task GenerateSlugAsync<T>(
        this T entity,
        string sourceText,
        DbContext dbContext,
        int maxLength = 80) where T : class, ISluggable
    {
        var baseSlug = SlugGenerator.GenerateSlug(sourceText, maxLength);
        
        var uniqueSlug = await SlugGenerator.GenerateUniqueSlugAsync(
            baseSlug,
            async (slug) => await dbContext.Set<T>().AnyAsync(e => e.Slug == slug)
        );

        entity.Slug = uniqueSlug;
    }

    /// <summary>
    /// Generates a slug without checking for uniqueness.
    /// </summary>
    /// <typeparam name="T">The entity type that implements ISluggable.</typeparam>
    /// <param name="entity">The entity to generate a slug for.</param>
    /// <param name="sourceText">The text to generate the slug from.</param>
    /// <param name="maxLength">Maximum length of the slug (default: 80).</param>
    /// <example>
    /// <code>
    /// var post = new BlogPost { Title = "My Awesome Post" };
    /// post.GenerateSlug(post.Title);
    /// // post.Slug is now "my-awesome-post"
    /// </code>
    /// </example>
    public static void GenerateSlug<T>(
        this T entity,
        string sourceText,
        int maxLength = 80) where T : class, ISluggable
    {
        entity.Slug = SlugGenerator.GenerateSlug(sourceText, maxLength);
    }

    /// <summary>
    /// Configures a unique index on the Slug property for all ISluggable entities.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ConfigureSlugIndexes();
    /// }
    /// </code>
    /// </example>
    public static void ConfigureSlugIndexes(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISluggable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex(nameof(ISluggable.Slug))
                    .IsUnique();
            }
        }
    }
}
