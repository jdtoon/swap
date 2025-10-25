public interface IGlobalSearchService
{
    Task<GlobalSearchResultDto> Search(string term);
} 