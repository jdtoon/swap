using Microsoft.AspNetCore.Mvc;

namespace habits.Controllers
{
    public class GlobalSearchController : Controller
    {
        private readonly IGlobalSearchService _searchService;

        public GlobalSearchController(IGlobalSearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task<IActionResult> Search(string search = "")
        {
            if (string.IsNullOrWhiteSpace(search))
                return PartialView("_GlobalSearchResults",
                    new GlobalSearchResultDto { SearchTerm = search });

            var results = await _searchService.Search(search);
            return PartialView("_GlobalSearchResults", results);
        }
    }
}