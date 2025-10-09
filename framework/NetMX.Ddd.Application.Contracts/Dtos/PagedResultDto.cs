namespace NetMX.Ddd.Application.Dtos;

public class PagedResultDto<T>
{
    public long TotalCount { get; set; }
    public IReadOnlyList<T> Items { get; set; }

    public PagedResultDto()
    {
    }

    public PagedResultDto(long totalCount, IReadOnlyList<T> items)
    {
        TotalCount = totalCount;
        Items = items;
    }
}