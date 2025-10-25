namespace KanbanApp.Dtos;

/// <summary>
/// Generic DTO for paginated results with "Load More" support (TTW pattern)
/// </summary>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public bool HasMore { get; set; }
    public int TotalRecords { get; set; }
    public int CurrentPage { get; set; }
}
