public class GlobalSearchResultDto
{
    public required string SearchTerm { get; set; }
    public List<SearchResultItem> Results { get; set; } = [];
}

public class SearchResultItem
{
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string NavigateUrl { get; set; }
    public int Priority { get; set; }
}
