namespace NetMX.Ddd.Application.Dtos;

/// <summary>
/// DTO for requesting paged results with skip count and maximum result count.
/// </summary>
public class PagedResultRequestDto
{
    /// <summary>
    /// Gets or sets the number of items to skip (for pagination).
    /// </summary>
    public virtual int SkipCount { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of items to return (page size). Defaults to 10.
    /// </summary>
    public virtual int MaxResultCount { get; set; } = 10;
}