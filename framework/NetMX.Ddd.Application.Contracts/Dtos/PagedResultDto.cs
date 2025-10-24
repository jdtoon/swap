namespace NetMX.Ddd.Application.Dtos;

/// <summary>
/// DTO for paged result sets containing a total count and a list of items.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public long TotalCount { get; set; }
    
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResultDto{T}"/> class.
    /// </summary>
    public PagedResultDto()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResultDto{T}"/> class with the specified total count and items.
    /// </summary>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="items">The items in the current page.</param>
    public PagedResultDto(long totalCount, IReadOnlyList<T> items)
    {
        TotalCount = totalCount;
        Items = items;
    }
}