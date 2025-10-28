using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Swap.Patterns.Sluggable;

/// <summary>
/// Utilities for generating URL-friendly slugs from strings.
/// </summary>
public static class SlugGenerator
{
    /// <summary>
    /// Generates a URL-friendly slug from the given text.
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <param name="maxLength">Maximum length of the slug (default: 80).</param>
    /// <returns>A URL-safe slug string.</returns>
    /// <example>
    /// <code>
    /// var slug = SlugGenerator.GenerateSlug("Hello World!"); // "hello-world"
    /// var slug = SlugGenerator.GenerateSlug("C# is Awesome"); // "csharp-is-awesome"
    /// </code>
    /// </example>
    public static string GenerateSlug(string text, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        var slug = text.ToLowerInvariant();

        // Remove diacritics (accents)
        slug = RemoveDiacritics(slug);

        // Replace invalid characters with hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

        // Convert multiple spaces or hyphens to single hyphen
        slug = Regex.Replace(slug, @"[\s-]+", " ").Trim();

        // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"\s", "-");

        // Trim to max length
        if (slug.Length > maxLength)
        {
            slug = slug.Substring(0, maxLength).Trim('-');
        }

        return slug;
    }

    /// <summary>
    /// Generates a unique slug by appending a counter if needed.
    /// </summary>
    /// <param name="baseSlug">The base slug to make unique.</param>
    /// <param name="existingSlugChecker">Function to check if a slug already exists.</param>
    /// <param name="maxAttempts">Maximum number of attempts to find a unique slug (default: 100).</param>
    /// <returns>A unique slug.</returns>
    /// <example>
    /// <code>
    /// var slug = await SlugGenerator.GenerateUniqueSlugAsync(
    ///     "my-post",
    ///     async (s) => await db.Posts.AnyAsync(p => p.Slug == s)
    /// );
    /// // Returns "my-post" if available, otherwise "my-post-2", "my-post-3", etc.
    /// </code>
    /// </example>
    public static async Task<string> GenerateUniqueSlugAsync(
        string baseSlug,
        Func<string, Task<bool>> existingSlugChecker,
        int maxAttempts = 100)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await existingSlugChecker(slug) && counter < maxAttempts)
        {
            counter++;
            slug = $"{baseSlug}-{counter}";
        }

        if (counter >= maxAttempts)
        {
            // Fallback to GUID suffix if we can't find a unique slug
            slug = $"{baseSlug}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        return slug;
    }

    /// <summary>
    /// Removes diacritics (accents) from characters.
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
