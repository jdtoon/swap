using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Models;

namespace SwapNavDemo.Controllers;

public class HomeController : SwapController
{
    public IActionResult Index()
    {
        return View();
    }

    #region Basic Navigation Examples

    /// <summary>
    /// Navigate to inbox - simplest form
    /// </summary>
    [HttpPost]
    public IActionResult GoToInbox()
    {
        return SwapResponse()
            .WithNavigation("/Home/Inbox")
            .Build();
    }

    /// <summary>
    /// Navigate to messages
    /// </summary>
    [HttpPost]
    public IActionResult GoToMessages()
    {
        return SwapResponse()
            .WithNavigation("/Home/Messages")
            .WithSuccessToast("Navigated to Messages")
            .Build();
    }

    /// <summary>
    /// Navigate to settings
    /// </summary>
    [HttpPost]
    public IActionResult GoToSettings()
    {
        return SwapResponse()
            .WithNavigation("/Home/Settings")
            .WithInfoToast("Settings loaded")
            .Build();
    }

    #endregion

    #region Content Pages (Navigation Targets)

    [HttpGet]
    public IActionResult Inbox()
    {
        var messages = new[]
        {
            new EmailMessage(1, "Welcome!", "sender@example.com", DateTime.Now.AddHours(-1), false),
            new EmailMessage(2, "Meeting Tomorrow", "boss@work.com", DateTime.Now.AddHours(-3), true),
            new EmailMessage(3, "Invoice #1234", "billing@vendor.com", DateTime.Now.AddDays(-1), false),
        };
        return PartialView("_Inbox", messages);
    }

    [HttpGet]
    public IActionResult Messages()
    {
        var messages = new[]
        {
            new ChatMessage(1, "Alice", "Hey, how's it going?", DateTime.Now.AddMinutes(-5)),
            new ChatMessage(2, "Bob", "Did you see the game last night?", DateTime.Now.AddMinutes(-30)),
            new ChatMessage(3, "Charlie", "Meeting at 3pm", DateTime.Now.AddHours(-2)),
        };
        return PartialView("_Messages", messages);
    }

    [HttpGet]
    public IActionResult Settings()
    {
        var settings = new UserSettings("john.doe", "john@example.com", true, "dark");
        return PartialView("_Settings", settings);
    }

    #endregion

    #region Custom Target Navigation

    /// <summary>
    /// Navigate content to sidebar instead of main area
    /// </summary>
    [HttpPost]
    public IActionResult LoadInSidebar()
    {
        return SwapResponse()
            .WithNavigation("/Home/SidebarContent", target: "#sidebar")
            .Build();
    }

    [HttpGet]
    public IActionResult SidebarContent()
    {
        return PartialView("_SidebarContent");
    }

    /// <summary>
    /// Navigate with custom swap mode via HxLocationOptions
    /// </summary>
    [HttpPost]
    public IActionResult AppendToList()
    {
        return SwapResponse()
            .WithNavigation(new HxLocationOptions
            {
                Path = "/Home/NewListItem",
                Target = "#item-list",
                Swap = "beforeend"
            })
            .Build();
    }

    [HttpGet]
    public IActionResult NewListItem()
    {
        var item = new ListItem(Random.Shared.Next(100, 999), $"Item added at {DateTime.Now:HH:mm:ss}");
        return PartialView("_ListItem", item);
    }

    #endregion

    #region Push URL Control

    /// <summary>
    /// Load modal content without pushing URL (user can't bookmark modal state)
    /// </summary>
    [HttpPost]
    public IActionResult LoadModalContent()
    {
        return SwapResponse()
            .WithNavigation("/Home/ModalContent", target: "#modal-container", pushUrl: false)
            .Build();
    }

    [HttpGet]
    public IActionResult ModalContent()
    {
        return PartialView("_ModalContent");
    }

    /// <summary>
    /// Load tab content without URL change
    /// </summary>
    [HttpPost]
    public IActionResult LoadTab(string tab)
    {
        return SwapResponse()
            .WithNavigation($"/Home/TabContent?tab={tab}", target: "#tab-content", pushUrl: false)
            .Build();
    }

    [HttpGet]
    public IActionResult TabContent(string tab)
    {
        return PartialView("_TabContent", tab);
    }

    #endregion

    #region Navigation with Form Data (HxLocationOptions.Values)

    /// <summary>
    /// Navigate with additional values (like search filters)
    /// </summary>
    [HttpPost]
    public IActionResult SearchAndNavigate(string query, string category)
    {
        return SwapResponse()
            .WithNavigation(new HxLocationOptions
            {
                Path = "/Home/SearchResults",
                Target = "#main-content",
                Values = new Dictionary<string, object?>
                {
                    ["query"] = query,
                    ["category"] = category
                }
            })
            .WithInfoToast($"Searching for '{query}' in {category}")
            .Build();
    }

    [HttpGet]
    public IActionResult SearchResults(string query, string category)
    {
        var results = new SearchResultsModel(query, category, new[]
        {
            $"Result 1 for '{query}'",
            $"Result 2 for '{query}'",
            $"Result 3 for '{query}' in {category}",
        });
        return PartialView("_SearchResults", results);
    }

    #endregion

    #region Chained Navigation Scenarios

    /// <summary>
    /// After form submit, navigate to a different page
    /// </summary>
    [HttpPost]
    public IActionResult SaveAndNavigate()
    {
        // Simulate saving...
        
        return SwapResponse()
            .WithNavigation("/Home/Inbox")
            .WithSuccessToast("Changes saved! Returning to inbox...")
            .Build();
    }

    /// <summary>
    /// Delete item then navigate back to list
    /// </summary>
    [HttpPost]
    public IActionResult DeleteAndNavigate(int id)
    {
        // Simulate delete...
        
        return SwapResponse()
            .WithNavigation("/Home/Messages")
            .WithSuccessToast($"Item {id} deleted")
            .WithTrigger("itemDeleted", new { id })
            .Build();
    }

    #endregion

    #region Comparison: WithRedirect vs WithNavigation

    /// <summary>
    /// Full page redirect (loses SPA state, full reload)
    /// </summary>
    [HttpPost]
    public IActionResult FullRedirect()
    {
        return SwapResponse()
            .WithRedirect("/Home/Index")
            .Build();
    }

    /// <summary>
    /// SPA navigation (preserves state, no reload)
    /// </summary>
    [HttpPost]
    public IActionResult SpaNavigate()
    {
        return SwapResponse()
            .WithNavigation("/Home/Inbox")
            .Build();
    }

    #endregion
}

// Models
public record EmailMessage(int Id, string Subject, string From, DateTime ReceivedAt, bool IsRead);
public record ChatMessage(int Id, string Sender, string Text, DateTime SentAt);
public record UserSettings(string Username, string Email, bool NotificationsEnabled, string Theme);
public record ListItem(int Id, string Name);
public record SearchResultsModel(string Query, string Category, string[] Results);
