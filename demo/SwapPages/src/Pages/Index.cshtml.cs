using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Swap.Htmx;

namespace SwapPages.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Count { get; set; }

    public void OnGet()
    {
        if (Count == 0) Count = 0;
    }

    public IActionResult OnGetUpdateCounter()
    {
        Count++;
        return this.SwapResponse()
            .WithView("_Counter", this)
            .Build();
    }
}
