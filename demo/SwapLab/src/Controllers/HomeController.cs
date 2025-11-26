using Microsoft.AspNetCore.Mvc;
using SwapLab.Models;

namespace SwapLab.Controllers;

public class HomeController : Controller
{
    private static readonly PatternInfo[] Patterns =
    [
        new("basic-swap", "Basic Swap", "Simple GET request with partial update", PatternCategories.Basics, ["hx-get", "hx-target"]),
        new("basic-post", "Form POST", "Submit a form and update the page", PatternCategories.Basics, ["hx-post", "form"]),
        new("oob-swap", "Out-of-Band Swaps", "Update multiple elements from one response", PatternCategories.Basics, ["AlsoUpdate", "hx-swap-oob"]),
        new("toasts", "Toast Notifications", "Show success, error, and custom toasts", PatternCategories.Basics, ["WithToast", "WithSuccessToast"]),
        new("loading-states", "Loading States", "Show spinners and disable during requests", PatternCategories.Basics, ["htmx-request", "hx-indicator"]),
        
        new("hidden-fields", "Hidden Field State", "Store state in hidden fields with hx-include", PatternCategories.State, ["hx-include", "hidden"]),
        new("url-state", "URL State", "Sync state with URL for bookmarkable pages", PatternCategories.State, ["hx-push-url", "query string"]),
        new("data-attributes", "Data Attribute State", "Store state in data-* attributes", PatternCategories.State, ["data-*", "hx-vals"]),
        
        new("event-chains", "Event Chains", "Configure cascading updates with events", PatternCategories.Events, ["ISwapEventConfiguration", "Trigger"]),
        new("event-timing", "Event Timing", "Understanding before-request vs after-request", PatternCategories.Events, ["hx-on", "timing"]),
        
        new("multi-component", "Multi-Component", "Tabs + Search + Pagination + Grid", PatternCategories.Components, ["coordination", "state"]),
        new("search-debounce", "Search with Debounce", "Real-time search with input debouncing", PatternCategories.Components, ["hx-trigger", "delay"]),
        new("infinite-scroll", "Infinite Scroll", "Load more content as user scrolls", PatternCategories.Components, ["hx-trigger", "revealed"]),
        
        new("form-validation", "Form Validation", "Server-side validation with inline errors", PatternCategories.Forms, ["validation", "blur"]),
        new("modal-forms", "Modal Forms", "Edit forms in modal dialogs", PatternCategories.Forms, ["modal", "dialog"]),
    ];

    public IActionResult Index()
    {
        var grouped = Patterns
            .GroupBy(p => p.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        return View(grouped);
    }

    public IActionResult Error()
    {
        return View();
    }
}
